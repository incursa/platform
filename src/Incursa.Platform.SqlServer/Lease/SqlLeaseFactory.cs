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


using Microsoft.Extensions.Logging;

namespace Incursa.Platform;
/// <summary>
/// SQL Server-based implementation of a system lease factory.
/// </summary>
internal sealed class SqlLeaseFactory : ISystemLeaseFactory
{
    private const int MaxAcquireAttempts = 3;
    private readonly LeaseFactoryConfig config;
    private readonly ILogger<SqlLeaseFactory> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlLeaseFactory"/> class.
    /// </summary>
    /// <param name="config">The lease configuration.</param>
    /// <param name="logger">The logger.</param>
    public SqlLeaseFactory(LeaseFactoryConfig config, ILogger<SqlLeaseFactory> logger)
    {
        this.config = config;
        this.logger = logger;
    }

    /// <inheritdoc/>
    public async Task<ISystemLease?> AcquireAsync(
        string resourceName,
        TimeSpan leaseDuration,
        string? contextJson = null,
        OwnerToken? ownerToken = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceName);

        // Create context with host information if none provided
        var finalContextJson = contextJson ?? CreateDefaultContext();

        for (var attempt = 1; attempt <= MaxAcquireAttempts; attempt++)
        {
            try
            {
                var lease = await SqlLease.AcquireAsync(
                    config.ConnectionString,
                    config.SchemaName,
                    resourceName,
                    leaseDuration,
                    config.RenewPercent,
                    config.UseGate,
                    config.GateTimeoutMs,
                    finalContextJson,
                    ownerToken,
                    cancellationToken,
                    logger).ConfigureAwait(false);

                if (lease != null)
                {
                    SchedulerMetrics.LeasesAcquired.Add(1, [new("resource", resourceName)]);
                }

                return lease;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex) when (SqlServerFailureClassifier.ShouldRetry(ex) && attempt < MaxAcquireAttempts)
            {
                var delay = ComputeRetryDelay(attempt);
                logger.LogWarning(
                    ex,
                    "Transient SQL failure ({FailureCategory}) acquiring lease for resource '{ResourceName}'. Retrying in {DelayMs}ms (attempt {Attempt}/{MaxAttempts})",
                    SqlServerFailureClassifier.GetCategoryName(ex),
                    resourceName,
                    delay.TotalMilliseconds,
                    attempt,
                    MaxAcquireAttempts);

                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (SqlServerFailureClassifier.IsInfrastructureFailure(ex))
            {
                logger.LogWarning(
                    ex,
                    "Infrastructure SQL failure ({FailureCategory}) acquiring lease for resource '{ResourceName}'",
                    SqlServerFailureClassifier.GetCategoryName(ex),
                    resourceName);
                throw;
            }
        }

        return null;
    }

    private static string CreateDefaultContext()
    {
        var context = new
        {
            Host = Environment.MachineName,
            ProcessId = Environment.ProcessId,
            AppVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown",
            InstanceId = Guid.NewGuid().ToString(),
            AcquiredAt = DateTimeOffset.UtcNow,
        };

        return JsonSerializer.Serialize(context);
    }

    private static TimeSpan ComputeRetryDelay(int attempt)
    {
        var cappedAttempt = Math.Min(8, Math.Max(1, attempt));
        var baseMs = (int)Math.Pow(2, cappedAttempt - 1) * 200;
        var jitter = Random.Shared.Next(0, 200);
        return TimeSpan.FromMilliseconds(Math.Min(5000, baseMs + jitter));
    }
}
