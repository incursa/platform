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

using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Incursa.Platform;

/// <summary>
/// Background service that processes global outbox messages.
/// </summary>
internal sealed class GlobalOutboxPollingService : BackgroundService
{
    private const int MaxFailureBackoffMs = 30_000;
    private readonly GlobalOutboxDispatcher dispatcher;
    private readonly IDatabaseSchemaCompletion? schemaCompletion;
    private readonly TimeSpan pollingInterval;
    private readonly ILogger<GlobalOutboxPollingService> logger;
    private readonly int batchSize;
    private int consecutiveFailureCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="GlobalOutboxPollingService"/> class.
    /// </summary>
    /// <param name="dispatcher">Global outbox dispatcher.</param>
    /// <param name="logger">Logger instance.</param>
    /// <param name="pollingInterval">Optional polling interval override.</param>
    /// <param name="schemaCompletion">Optional schema completion notifier.</param>
    /// <param name="batchSize">Optional batch size override.</param>
    public GlobalOutboxPollingService(
        GlobalOutboxDispatcher dispatcher,
        ILogger<GlobalOutboxPollingService> logger,
        TimeSpan? pollingInterval = null,
        IDatabaseSchemaCompletion? schemaCompletion = null,
        int? batchSize = null)
    {
        this.dispatcher = dispatcher;
        this.logger = logger;
        this.schemaCompletion = schemaCompletion;
        this.pollingInterval = pollingInterval ?? TimeSpan.FromSeconds(5);
        this.batchSize = batchSize ?? 50;
    }

    /// <inheritdoc/>
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Polling loop logs and continues on failures.")]
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "Starting global outbox polling service with {IntervalSeconds}s interval",
            pollingInterval.TotalSeconds);

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
                logger.LogWarning(ex, "Schema deployment failed, but continuing with global outbox processing");
            }
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = pollingInterval;
            try
            {
                var processedCount = await dispatcher.RunOnceAsync(batchSize, stoppingToken).ConfigureAwait(false);
                consecutiveFailureCount = 0;
                if (processedCount > 0)
                {
                    logger.LogDebug(
                        "Global outbox polling iteration completed: {ProcessedCount} messages processed",
                        processedCount);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                logger.LogDebug("Global outbox polling service stopped due to cancellation");
                break;
            }
            catch (Exception ex) when (ExceptionFilter.IsCatchable(ex))
            {
                consecutiveFailureCount++;
                var failureBackoff = ComputeFailureBackoff(consecutiveFailureCount);
                if (failureBackoff > delay)
                {
                    delay = failureBackoff;
                }

                logger.LogWarning(
                    ex,
                    "Error in global outbox polling iteration. Applying {BackoffMs}ms failure backoff before retrying",
                    failureBackoff.TotalMilliseconds);
            }

            await Task.Delay(delay, stoppingToken).ConfigureAwait(false);
        }

        logger.LogInformation("Global outbox polling service stopped");
    }

    private static TimeSpan ComputeFailureBackoff(int failureCount)
    {
        var cappedFailureCount = Math.Min(10, Math.Max(1, failureCount));
        var baseMs = (int)Math.Pow(2, cappedFailureCount - 1) * 250;
        var jitter = RandomNumberGenerator.GetInt32(0, 250);
        return TimeSpan.FromMilliseconds(Math.Min(MaxFailureBackoffMs, baseMs + jitter));
    }
}
