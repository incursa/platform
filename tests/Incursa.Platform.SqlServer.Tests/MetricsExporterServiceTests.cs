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

using Incursa.Platform.Metrics;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Incursa.Platform.Tests;

public sealed class MetricsExporterServiceTests
{
    [Fact]
    public void RecordMeasurementForTesting_WithEquivalentTagContent_UsesSingleMinuteSeries()
    {
        var service = CreateService(options =>
        {
            options.EnableCentralRollup = false;
            options.MaxMinuteSeries = 100;
        });

        service.RecordMeasurementForTesting(
            metricName: "test.metric",
            unit: MetricUnit.Count,
            aggKind: MetricAggregationKind.Counter,
            description: "test",
            databaseId: Guid.Empty,
            service: "svc",
            tags: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["queue"] = "inbox",
                ["store"] = "infra",
            },
            value: 1);
        service.RecordMeasurementForTesting(
            metricName: "test.metric",
            unit: MetricUnit.Count,
            aggKind: MetricAggregationKind.Counter,
            description: "test",
            databaseId: Guid.Empty,
            service: "svc",
            tags: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["store"] = "infra",
                ["queue"] = "inbox",
            },
            value: 1);

        service.MinuteSeriesCount.ShouldBe(1);
        service.DroppedMinuteSeriesCount.ShouldBe(0);
    }

    [Fact]
    public void RecordMeasurementForTesting_WhenMinuteCapReached_DropsNewSeries()
    {
        var service = CreateService(options =>
        {
            options.EnableCentralRollup = false;
            options.MaxMinuteSeries = 1;
        });

        service.RecordMeasurementForTesting(
            metricName: "test.metric",
            unit: MetricUnit.Count,
            aggKind: MetricAggregationKind.Counter,
            description: "test",
            databaseId: Guid.Empty,
            service: "svc",
            tags: new Dictionary<string, string>(StringComparer.Ordinal) { ["queue"] = "inbox-a" },
            value: 1);
        service.RecordMeasurementForTesting(
            metricName: "test.metric",
            unit: MetricUnit.Count,
            aggKind: MetricAggregationKind.Counter,
            description: "test",
            databaseId: Guid.Empty,
            service: "svc",
            tags: new Dictionary<string, string>(StringComparer.Ordinal) { ["queue"] = "inbox-b" },
            value: 1);

        service.MinuteSeriesCount.ShouldBe(1);
        service.DroppedMinuteSeriesCount.ShouldBe(1);
    }

    [Fact]
    public void RecordMeasurementForTesting_WhenHourlyCapReached_DropsNewHourlySeries()
    {
        var service = CreateService(options =>
        {
            options.EnableCentralRollup = true;
            options.CentralConnectionString = "Server=localhost;Database=metrics;";
            options.MaxMinuteSeries = 100;
            options.MaxHourlySeries = 1;
        });

        service.RecordMeasurementForTesting(
            metricName: "test.metric",
            unit: MetricUnit.Count,
            aggKind: MetricAggregationKind.Counter,
            description: "test",
            databaseId: Guid.Empty,
            service: "svc",
            tags: new Dictionary<string, string>(StringComparer.Ordinal) { ["queue"] = "inbox-a" },
            value: 1);
        service.RecordMeasurementForTesting(
            metricName: "test.metric",
            unit: MetricUnit.Count,
            aggKind: MetricAggregationKind.Counter,
            description: "test",
            databaseId: Guid.Empty,
            service: "svc",
            tags: new Dictionary<string, string>(StringComparer.Ordinal) { ["queue"] = "inbox-b" },
            value: 1);

        service.HourlySeriesCount.ShouldBe(1);
        service.DroppedHourlySeriesCount.ShouldBe(1);
        service.MinuteSeriesCount.ShouldBe(2);
    }

    [Fact]
    public void IsFatalException_RecognizesOutOfMemory()
    {
        MetricsExporterService.IsFatalException(new OutOfMemoryException()).ShouldBeTrue();
        MetricsExporterService.IsFatalException(new InvalidOperationException("not fatal")).ShouldBeFalse();
    }

    [Fact]
    public void MetricsExporterOptions_DefaultSeriesCaps_AreConfigured()
    {
        var options = new MetricsExporterOptions();

        options.MaxMinuteSeries.ShouldBe(12000);
        options.MaxHourlySeries.ShouldBe(12000);
        options.SeriesCapWarningInterval.ShouldBe(TimeSpan.FromMinutes(1));
    }

    private static MetricsExporterService CreateService(Action<MetricsExporterOptions>? configure = null)
    {
        var options = new MetricsExporterOptions
        {
            Enabled = true,
            ServiceName = "svc",
            EnableCentralRollup = false,
        };
        configure?.Invoke(options);

        var discovery = new ListBasedDatabaseDiscovery(
            new[]
            {
                new PlatformDatabase
                {
                    Name = "default",
                    ConnectionString = "Server=localhost;Database=metrics;",
                    SchemaName = "infra",
                },
            });

        var registrar = new MetricRegistrar(new NullLogger<MetricRegistrar>());
        return new MetricsExporterService(
            new NullLogger<MetricsExporterService>(),
            Options.Create(options),
            discovery,
            registrar);
    }
}
