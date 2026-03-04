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

using System.Security.Cryptography;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Incursa.Platform;
/// <summary>
/// Background service that periodically polls and processes outbox messages from multiple databases.
/// Uses monotonic clock for consistent polling intervals regardless of system time changes.
/// Waits for database schema deployment to complete before starting.
/// </summary>
internal sealed class MultiOutboxPollingService : BackgroundService
{
    private const int MaxFailureBackoffMs = 30_000;
    private readonly MultiOutboxDispatcher dispatcher;
    private readonly IMonotonicClock mono;
    private readonly IDatabaseSchemaCompletion? schemaCompletion;
    private readonly double intervalSeconds;
    private readonly int batchSize;
    private readonly ILogger<MultiOutboxPollingService> logger;
    private int consecutiveFailureCount;

    public MultiOutboxPollingService(
        MultiOutboxDispatcher dispatcher,
        IMonotonicClock mono,
        ILogger<MultiOutboxPollingService> logger,
        double intervalSeconds = 0.25, // 250ms default
        int batchSize = 50,
        IDatabaseSchemaCompletion? schemaCompletion = null)
    {
        this.dispatcher = dispatcher;
        this.mono = mono;
        this.logger = logger;
        this.schemaCompletion = schemaCompletion;
        this.intervalSeconds = intervalSeconds;
        this.batchSize = batchSize;
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Polling loop logs and continues on failures.")]
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "Starting multi-outbox polling service with {IntervalMs}ms interval and batch size {BatchSize}",
            intervalSeconds * 1000, batchSize);

        // Wait for schema deployment to complete if available
        if (schemaCompletion != null)
        {
            logger.LogDebug("Waiting for database schema deployment to complete");
            try
            {
                await schemaCompletion.SchemaDeploymentCompleted.ConfigureAwait(false);
                logger.LogInformation("Database schema deployment completed successfully");
            }
            catch (Exception ex) when (ExceptionFilter.IsCatchable(ex))
            {
                // Log and continue - schema deployment errors should not prevent outbox processing
                logger.LogWarning(ex, "Schema deployment failed, but continuing with outbox processing");
            }
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            var next = mono.Seconds + intervalSeconds;
            var failureBackoff = TimeSpan.Zero;

            try
            {
                var processedCount = await dispatcher.RunOnceAsync(batchSize, stoppingToken).ConfigureAwait(false);
                consecutiveFailureCount = 0;

                if (processedCount > 0)
                {
                    logger.LogDebug("Multi-outbox polling iteration completed: {ProcessedCount} messages processed", processedCount);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                logger.LogDebug("Multi-outbox polling service stopped due to cancellation");
                break;
            }
            catch (Exception ex) when (ExceptionFilter.IsCatchable(ex))
            {
                consecutiveFailureCount++;
                failureBackoff = ComputeFailureBackoff(consecutiveFailureCount);
                logger.LogWarning(
                    ex,
                    "Error in multi-outbox polling iteration. Applying {BackoffMs}ms failure backoff before retrying",
                    failureBackoff.TotalMilliseconds);
            }

            // Sleep until next interval, using monotonic clock to avoid time jumps
            var sleep = Math.Max(0, next - mono.Seconds);
            if (failureBackoff > TimeSpan.Zero)
            {
                sleep = Math.Max(sleep, failureBackoff.TotalSeconds);
            }

            if (sleep > 0)
            {
                await Task.Delay(TimeSpan.FromSeconds(sleep), stoppingToken).ConfigureAwait(false);
            }
        }

        logger.LogInformation("Multi-outbox polling service stopped");
    }

    private static TimeSpan ComputeFailureBackoff(int failureCount)
    {
        var cappedFailureCount = Math.Min(10, Math.Max(1, failureCount));
        var baseMs = (int)Math.Pow(2, cappedFailureCount - 1) * 250;
        var jitter = RandomNumberGenerator.GetInt32(0, 250);
        return TimeSpan.FromMilliseconds(Math.Min(MaxFailureBackoffMs, baseMs + jitter));
    }
}
