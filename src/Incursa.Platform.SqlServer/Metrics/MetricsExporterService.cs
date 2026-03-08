// Copyright (c) Incursa
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.


using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics.Metrics;
using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Incursa.Platform.Metrics;
/// <summary>
/// Background service that exports metrics to SQL Server databases.
/// </summary>
internal sealed class MetricsExporterService : BackgroundService
{
    private static readonly IReadOnlyDictionary<string, string> EmptyTags =
        new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(StringComparer.Ordinal));

    private readonly ILogger<MetricsExporterService> _logger;
    private readonly MetricsExporterOptions _options;
    private readonly IPlatformDatabaseDiscovery _databaseDiscovery;
    private readonly MetricRegistrar? _metricRegistrar;
    private readonly MeterListener _listener;
    private readonly Guid _instanceId;
    private readonly ConcurrentDictionary<string, MetricDefinition> _metricDefinitions;
    private readonly ConcurrentDictionary<SeriesLookupKey, SeriesAggregationState> _minuteAggregators;
    private readonly ConcurrentDictionary<SeriesLookupKey, SeriesAggregationState> _hourlyAggregators;
    private readonly Lock _seriesDropLogLock = new();
    private DateTime? _lastSeriesDropLogUtc;
    private DateTime? _lastFlushUtc;
    private DateTime? _lastHourlyFlushUtc;
    private string? _lastError;
    private long _droppedMinuteSeriesCount;
    private long _droppedHourlySeriesCount;

    public MetricsExporterService(
        ILogger<MetricsExporterService> logger,
        IOptions<MetricsExporterOptions> options,
        IPlatformDatabaseDiscovery databaseDiscovery,
        MetricRegistrar metricRegistrar)
    {
        _logger = logger;
        _options = options.Value;
        _databaseDiscovery = databaseDiscovery;
        _metricRegistrar = metricRegistrar ?? throw new ArgumentNullException(nameof(metricRegistrar));
        _instanceId = Guid.NewGuid();
        _metricDefinitions = new ConcurrentDictionary<string, MetricDefinition>(StringComparer.Ordinal);
        _minuteAggregators = new ConcurrentDictionary<SeriesLookupKey, SeriesAggregationState>();
        _hourlyAggregators = new ConcurrentDictionary<SeriesLookupKey, SeriesAggregationState>();

        _listener = new MeterListener
        {
            InstrumentPublished = (instrument, listener) =>
            {
                // Only subscribe to Incursa.Platform meters
                if (instrument.Meter.Name.StartsWith("Incursa.Platform", StringComparison.Ordinal))
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            },
        };

        _listener.SetMeasurementEventCallback<double>(OnMeasurementRecorded);
        _listener.SetMeasurementEventCallback<long>(OnMeasurementRecorded);
        _listener.SetMeasurementEventCallback<int>(OnMeasurementRecorded);
    }

    /// <summary>
    /// Gets the last flush time (UTC).
    /// </summary>
    public DateTime? LastFlushUtc => _lastFlushUtc;

    /// <summary>
    /// Gets the last error message.
    /// </summary>
    public string? LastError => _lastError;

    /// <summary>
    /// Gets the current minute-series count retained in memory.
    /// </summary>
    public int MinuteSeriesCount => _minuteAggregators.Count;

    /// <summary>
    /// Gets the current hourly-series count retained in memory.
    /// </summary>
    public int HourlySeriesCount => _hourlyAggregators.Count;

    /// <summary>
    /// Gets the total number of dropped minute-series admissions.
    /// </summary>
    public long DroppedMinuteSeriesCount => Interlocked.Read(ref _droppedMinuteSeriesCount);

    /// <summary>
    /// Gets the total number of dropped hourly-series admissions.
    /// </summary>
    public long DroppedHourlySeriesCount => Interlocked.Read(ref _droppedHourlySeriesCount);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Metrics exporter is disabled");
            return;
        }

        _listener.Start();
        _logger.LogInformation("Metrics exporter started with instance ID {InstanceId}", _instanceId);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_options.FlushInterval, stoppingToken).ConfigureAwait(false);
                await FlushMinuteMetricsAsync(stoppingToken).ConfigureAwait(false);
                await CheckHourlyFlushAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Expected during shutdown
                break;
            }
            catch (Exception ex) when (!IsFatalException(ex))
            {
                _lastError = ex.ToString();
                _logger.LogError(ex, "Error during metrics flush");
            }
        }

        _listener.Dispose();
        _logger.LogInformation("Metrics exporter stopped");
    }

    private void OnMeasurementRecorded<T>(Instrument instrument, T measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state)
        where T : struct
    {
        try
        {
            var value = Convert.ToDouble(measurement, System.Globalization.CultureInfo.InvariantCulture);
            var metricName = instrument.Name;
            var unit = instrument.Unit ?? "count";

            // Extract database ID and service from tags
            Guid? databaseId = null;
            var service = _options.ServiceName;
            var filteredTags = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (var tag in tags)
            {
                if (tag.Key.Equals("database_id", StringComparison.Ordinal) && tag.Value is Guid guid)
                {
                    databaseId = guid;
                }
                else if (tag.Key.Equals("service", StringComparison.Ordinal) && tag.Value is string svc)
                {
                    service = svc;
                }
                else if (tag.Value is string strValue && IsAllowedTag(metricName, tag.Key))
                {
                    filteredTags[tag.Key] = strValue;
                }
            }

            // Determine aggregation kind
            var aggKind = DetermineAggregationKind(instrument);
            var description = instrument.Description ?? metricName;
            RecordMeasurement(metricName, unit, aggKind, description, databaseId, service, filteredTags, value);
        }
        catch (Exception ex) when (!IsFatalException(ex))
        {
            _logger.LogWarning(ex, "Error recording measurement for {InstrumentName}", instrument.Name);
        }
    }

    internal static bool IsFatalException(Exception ex)
    {
        return ex is OutOfMemoryException
            or StackOverflowException
            or AccessViolationException
            or AppDomainUnloadedException
            or BadImageFormatException;
    }

    internal void RecordMeasurementForTesting(
        string metricName,
        string unit,
        MetricAggregationKind aggKind,
        string description,
        Guid? databaseId,
        string service,
        IReadOnlyDictionary<string, string> tags,
        double value)
    {
        RecordMeasurement(metricName, unit, aggKind, description, databaseId, service, tags, value);
    }

    private void RecordMeasurement(
        string metricName,
        string unit,
        MetricAggregationKind aggKind,
        string description,
        Guid? databaseId,
        string service,
        IReadOnlyDictionary<string, string> tags,
        double value)
    {
        var normalizedDatabaseId = databaseId ?? Guid.Empty;
        var canonicalTagSignature = BuildTagSignature(tags);
        var lookupKey = new SeriesLookupKey(
            metricName,
            normalizedDatabaseId,
            service,
            _instanceId,
            canonicalTagSignature);

        _metricDefinitions.TryAdd(metricName, new MetricDefinition
        {
            Name = metricName,
            Unit = unit,
            AggKind = aggKind,
            Description = description,
        });

        if (!TryRecordValue(_minuteAggregators, lookupKey, metricName, normalizedDatabaseId, service, tags, value, _options.MaxMinuteSeries, isHourly: false))
        {
            return;
        }

        if (_options.EnableCentralRollup && !string.IsNullOrEmpty(_options.CentralConnectionString))
        {
            _ = TryRecordValue(_hourlyAggregators, lookupKey, metricName, normalizedDatabaseId, service, tags, value, _options.MaxHourlySeries, isHourly: true);
        }
    }

    private bool TryRecordValue(
        ConcurrentDictionary<SeriesLookupKey, SeriesAggregationState> store,
        SeriesLookupKey lookupKey,
        string metricName,
        Guid databaseId,
        string service,
        IReadOnlyDictionary<string, string> tags,
        double value,
        int configuredCap,
        bool isHourly)
    {
        if (store.TryGetValue(lookupKey, out var existingState))
        {
            existingState.Aggregator.Record(value);
            return true;
        }

        var cap = configuredCap <= 0 ? int.MaxValue : configuredCap;
        if (store.Count >= cap)
        {
            TrackSeriesDrop(isHourly, cap);
            return false;
        }

        var canonicalTags = CreateCanonicalTags(tags);
        var seriesKey = new MetricSeriesKey
        {
            MetricName = metricName,
            DatabaseId = databaseId,
            Service = service,
            InstanceId = _instanceId,
            Tags = canonicalTags,
        };

        var addedState = store.GetOrAdd(
            lookupKey,
            _ => new SeriesAggregationState(seriesKey, new MetricAggregator(_options.ReservoirSize)));
        addedState.Aggregator.Record(value);
        return true;
    }

    private void TrackSeriesDrop(bool isHourly, int cap)
    {
        if (isHourly)
        {
            Interlocked.Increment(ref _droppedHourlySeriesCount);
        }
        else
        {
            Interlocked.Increment(ref _droppedMinuteSeriesCount);
        }

        var now = DateTime.UtcNow;
        var interval = _options.SeriesCapWarningInterval <= TimeSpan.Zero
            ? TimeSpan.FromMinutes(1)
            : _options.SeriesCapWarningInterval;

        lock (_seriesDropLogLock)
        {
            if (_lastSeriesDropLogUtc.HasValue && now - _lastSeriesDropLogUtc.Value < interval)
            {
                return;
            }

            _lastSeriesDropLogUtc = now;
            _logger.LogWarning(
                "Metrics series cap reached for {Level} aggregation. New series are being dropped. Cap={Cap}, MinuteSeriesCount={MinuteSeriesCount}, HourlySeriesCount={HourlySeriesCount}, DroppedMinuteSeries={DroppedMinuteSeriesCount}, DroppedHourlySeries={DroppedHourlySeriesCount}",
                isHourly ? "hourly" : "minute",
                cap,
                MinuteSeriesCount,
                HourlySeriesCount,
                DroppedMinuteSeriesCount,
                DroppedHourlySeriesCount);
        }
    }

    private static IReadOnlyDictionary<string, string> CreateCanonicalTags(IReadOnlyDictionary<string, string> tags)
    {
        if (tags.Count == 0)
        {
            return EmptyTags;
        }

        var ordered = tags
            .OrderBy(kvp => kvp.Key, StringComparer.Ordinal)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.Ordinal);
        return new ReadOnlyDictionary<string, string>(ordered);
    }

    private static string BuildTagSignature(IReadOnlyDictionary<string, string> tags)
    {
        if (tags.Count == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        foreach (var tag in tags.OrderBy(kvp => kvp.Key, StringComparer.Ordinal))
        {
            builder.Append(tag.Key.Length);
            builder.Append(':');
            builder.Append(tag.Key);
            builder.Append('=');
            builder.Append(tag.Value.Length);
            builder.Append(':');
            builder.Append(tag.Value);
            builder.Append(';');
        }

        return builder.ToString();
    }

    private static MetricAggregationKind DetermineAggregationKind(Instrument instrument)
    {
        return instrument switch
        {
            Counter<long> => MetricAggregationKind.Counter,
            Counter<int> => MetricAggregationKind.Counter,
            Counter<double> => MetricAggregationKind.Counter,
            ObservableCounter<long> => MetricAggregationKind.Counter,
            ObservableCounter<int> => MetricAggregationKind.Counter,
            ObservableCounter<double> => MetricAggregationKind.Counter,
            Histogram<long> => MetricAggregationKind.Histogram,
            Histogram<int> => MetricAggregationKind.Histogram,
            Histogram<double> => MetricAggregationKind.Histogram,
            _ => MetricAggregationKind.Gauge,
        };
    }

    private bool IsAllowedTag(string metricName, string tagKey)
    {
        // First check if the metric has specific allowed tags via registrar
        if (_metricRegistrar != null && _metricRegistrar.IsTagAllowed(metricName, tagKey))
        {
            return true;
        }

        // Fall back to global allowed tags
        return _options.GlobalAllowedTags.Contains(tagKey);
    }

    private async Task FlushMinuteMetricsAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var bucketStart = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0, DateTimeKind.Utc);

        var databases = await _databaseDiscovery.DiscoverDatabasesAsync(cancellationToken).ConfigureAwait(false);

        // Group aggregators by database
        var aggregatorsByDatabase = _minuteAggregators.Values
            .GroupBy(state => state.SeriesKey.DatabaseId)
            .ToList();

        foreach (var databaseGroup in aggregatorsByDatabase)
        {
            // For now, write to the first database (or a default database)
            // In a multi-database setup, you'd map databaseId to the appropriate database
            var targetDb = databases.FirstOrDefault();
            if (targetDb == null || string.IsNullOrEmpty(targetDb.ConnectionString))
            {
                continue;
            }

            await FlushDatabaseGroupAsync(databaseGroup, targetDb.ConnectionString, bucketStart, cancellationToken).ConfigureAwait(false);
        }

        _lastFlushUtc = DateTime.UtcNow;
        await UpdateHeartbeatAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task FlushDatabaseGroupAsync(
        IGrouping<Guid?, SeriesAggregationState> databaseGroup,
        string connectionString,
        DateTime bucketStart,
        CancellationToken cancellationToken)
    {
        foreach (var state in databaseGroup)
        {
            var seriesKey = state.SeriesKey;
            var snapshot = state.Aggregator.GetSnapshotAndReset();

            if (snapshot.Count == 0)
            {
                continue; // Skip empty snapshots
            }

            try
            {
                if (_metricDefinitions.TryGetValue(seriesKey.MetricName, out var metricDef))
                {
                    await SqlMetricsWriter.WriteMinutePointAsync(
                        connectionString,
                        _options.SchemaName,
                        seriesKey,
                        snapshot,
                        bucketStart,
                        metricDef.Unit,
                        metricDef.AggKind.ToString().ToUpperInvariant(),
                        metricDef.Description,
                        cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception ex) when (!IsFatalException(ex))
            {
                _logger.LogWarning(ex, "Error writing minute metric for series {MetricName}", seriesKey.MetricName);
            }
        }
    }

    private async Task UpdateHeartbeatAsync(CancellationToken cancellationToken)
    {
        // Update heartbeat in central DB if configured
        if (!string.IsNullOrEmpty(_options.CentralConnectionString))
        {
            try
            {
                await SqlMetricsWriter.UpdateHeartbeatAsync(
                    _options.CentralConnectionString,
                    _options.SchemaName,
                    _instanceId.ToString(),
                    _lastFlushUtc!.Value,
                    _lastError,
                    cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (!IsFatalException(ex))
            {
                _logger.LogWarning(ex, "Error updating heartbeat");
            }
        }
    }

    private async Task CheckHourlyFlushAsync(CancellationToken cancellationToken)
    {
        if (!_options.EnableCentralRollup || string.IsNullOrEmpty(_options.CentralConnectionString))
        {
            return;
        }

        var now = DateTime.UtcNow;
        var currentHour = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, DateTimeKind.Utc);

        // Flush once per hour when we cross into a new hour
        if (_lastHourlyFlushUtc == null || _lastHourlyFlushUtc.Value < currentHour)
        {
            await FlushHourlyMetricsAsync(cancellationToken).ConfigureAwait(false);
            _lastHourlyFlushUtc = currentHour;
        }
    }

    private async Task FlushHourlyMetricsAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var bucketStart = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, DateTimeKind.Utc).AddHours(-1);

        foreach (var state in _hourlyAggregators.Values)
        {
            var seriesKey = state.SeriesKey;
            var snapshot = state.Aggregator.GetSnapshotAndReset();

            if (snapshot.Count == 0)
            {
                continue;
            }

            try
            {
                if (_metricDefinitions.TryGetValue(seriesKey.MetricName, out var metricDef))
                {
                    await SqlMetricsWriter.WriteHourlyPointAsync(
                        _options.CentralConnectionString!,
                        _options.SchemaName,
                        seriesKey,
                        snapshot,
                        bucketStart,
                        metricDef.Unit,
                        metricDef.AggKind.ToString().ToUpperInvariant(),
                        metricDef.Description,
                        cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception ex) when (!IsFatalException(ex))
            {
                _logger.LogWarning(ex, "Error writing hourly metric for series {MetricName}", seriesKey.MetricName);
            }
        }
    }

    public override void Dispose()
    {
        _listener.Dispose();
        base.Dispose();
    }

    private sealed record SeriesLookupKey(
        string MetricName,
        Guid DatabaseId,
        string Service,
        Guid InstanceId,
        string TagSignature);

    private sealed class SeriesAggregationState
    {
        public SeriesAggregationState(MetricSeriesKey seriesKey, MetricAggregator aggregator)
        {
            SeriesKey = seriesKey;
            Aggregator = aggregator;
        }

        public MetricSeriesKey SeriesKey { get; }

        public MetricAggregator Aggregator { get; }
    }
}
