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

using System.Data.Common;
using Cronos;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Incursa.Platform;
/// <summary>
/// SQL Server implementation of ISchedulerStore.
/// Provides scheduler operations for a specific database instance.
/// </summary>
internal sealed class SqlSchedulerStore : ISchedulerStore
{
    private readonly string connectionString;
    private readonly SqlSchedulerOptions options;
    private readonly TimeProvider timeProvider;
    private readonly string instanceId = $"{Environment.MachineName}:{Guid.NewGuid()}";

    // Pre-built SQL queries using configured table names
    private readonly string claimTimersSql;
    private readonly string claimJobsSql;
    private readonly string getNextEventTimeSql;
    private readonly string schedulerStateUpdateSql;

    public SqlSchedulerStore(IOptions<SqlSchedulerOptions> options, TimeProvider timeProvider)
    {
        this.options = options.Value;
        connectionString = this.options.ConnectionString;
        this.timeProvider = timeProvider;

        // Build SQL queries using configured schema and table names
        claimTimersSql = $"""

                        UPDATE [{this.options.SchemaName}].[{this.options.TimersTableName}]
                        SET Status = 'Claimed', ClaimedBy = @InstanceId, ClaimedAt = SYSDATETIMEOFFSET()
                        OUTPUT INSERTED.Id, INSERTED.Topic, INSERTED.Payload
                        WHERE Id IN (
                            SELECT TOP (@BatchSize) Id FROM [{this.options.SchemaName}].[{this.options.TimersTableName}]
                            WHERE Status = 'Pending' AND DueTime <= SYSDATETIMEOFFSET()
                              AND @FencingToken >= (SELECT ISNULL(CurrentFencingToken, 0) FROM [{this.options.SchemaName}].[SchedulerState] WHERE Id = 1)
                            ORDER BY DueTime
                        );
            """;

        claimJobsSql = $"""

                        UPDATE [{this.options.SchemaName}].[{this.options.JobRunsTableName}]
                        SET Status = 'Claimed', ClaimedBy = @InstanceId, ClaimedAt = SYSDATETIMEOFFSET()
                        OUTPUT INSERTED.Id, INSERTED.JobId, j.Topic, j.Payload
                        FROM [{this.options.SchemaName}].[{this.options.JobRunsTableName}] jr
                        INNER JOIN [{this.options.SchemaName}].[{this.options.JobsTableName}] j ON jr.JobId = j.Id
                        WHERE jr.Id IN (
                            SELECT TOP (@BatchSize) Id FROM [{this.options.SchemaName}].[{this.options.JobRunsTableName}]
                            WHERE Status = 'Pending' AND ScheduledTime <= SYSDATETIMEOFFSET()
                              AND @FencingToken >= (SELECT ISNULL(CurrentFencingToken, 0) FROM [{this.options.SchemaName}].[SchedulerState] WHERE Id = 1)
                            ORDER BY ScheduledTime
                        );
            """;

        getNextEventTimeSql = $"""

                        SELECT MIN(NextDue)
                        FROM (
                            SELECT MIN(DueTime) AS NextDue FROM [{this.options.SchemaName}].[{this.options.TimersTableName}] WHERE Status = 'Pending'
                            UNION ALL
                            SELECT MIN(ScheduledTime) AS NextDue FROM [{this.options.SchemaName}].[{this.options.JobRunsTableName}] WHERE Status = 'Pending'
                            UNION ALL
                            SELECT MIN(NextDueTime) AS NextDue FROM [{this.options.SchemaName}].[{this.options.JobsTableName}]
                        ) AS NextEvents;
            """;

        // SQL to update the fencing token state for scheduler operations
        schedulerStateUpdateSql = $"""

                        MERGE [{this.options.SchemaName}].[SchedulerState] AS target
                        USING (VALUES (1, @FencingToken, @LastRunAt)) AS source (Id, FencingToken, LastRunAt)
                        ON target.Id = source.Id
                        WHEN MATCHED AND @FencingToken >= target.CurrentFencingToken THEN
                            UPDATE SET CurrentFencingToken = @FencingToken, LastRunAt = @LastRunAt
                        WHEN NOT MATCHED THEN
                            INSERT (Id, CurrentFencingToken, LastRunAt) VALUES (1, @FencingToken, @LastRunAt);
            """;
    }

    public async Task<DateTimeOffset?> GetNextEventTimeAsync(CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var command = new CommandDefinition(
            getNextEventTimeSql,
            cancellationToken: cancellationToken);
        return await connection.ExecuteScalarAsync<DateTimeOffset?>(command).ConfigureAwait(false);
    }

    public async Task<int> CreateJobRunsFromDueJobsAsync(ISystemLease lease, CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            lease.ThrowIfLost();

            var findDueJobsSql = $"""

                        SELECT Id, CronSchedule FROM [{options.SchemaName}].[{options.JobsTableName}]
                        WHERE NextDueTime <= @Now;
            """;

            var dbConnection = transaction.Connection ?? throw new InvalidOperationException("Transaction connection was not available.");
            var dueJobsCommand = new CommandDefinition(
                findDueJobsSql,
                new { Now = timeProvider.GetUtcNow() },
                transaction,
                cancellationToken: cancellationToken);
            var dueJobs = (await dbConnection.QueryAsync<(Guid Id, string CronSchedule)>(dueJobsCommand).ConfigureAwait(false)).AsList();

            if (dueJobs.Count == 0)
            {
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                return 0;
            }

            lease.ThrowIfLost();

            var runsToInsert = new List<object>();
            var jobsToUpdate = new List<object>();
            var now = timeProvider.GetUtcNow();

            foreach (var job in dueJobs)
            {
                // Prepare the new JobRun record
                runsToInsert.Add(new
                {
                    RunId = Guid.NewGuid(),
                    JobId = job.Id,
                    ScheduledTime = now,
                });

                // Determine cron format based on the number of parts.
                var format = job.CronSchedule.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length == 6
                    ? CronFormat.IncludeSeconds
                    : CronFormat.Standard;

                // Calculate the next occurrence and prepare the update
                var cronExpression = CronExpression.Parse(job.CronSchedule, format);
                var nextOccurrence = cronExpression.GetNextOccurrence(now.UtcDateTime);
                jobsToUpdate.Add(new
                {
                    NextDueTime = nextOccurrence,
                    JobId = job.Id,
                });
            }

            var insertRunSql = $"""

                        INSERT INTO [{options.SchemaName}].[{options.JobRunsTableName}] (Id, JobId, ScheduledTime, Status)
                        VALUES (@RunId, @JobId, @ScheduledTime, 'Pending');
            """;

            var insertCommand = new CommandDefinition(
                insertRunSql,
                runsToInsert,
                transaction,
                cancellationToken: cancellationToken);
            await transaction.Connection.ExecuteAsync(insertCommand).ConfigureAwait(false);

            var updateJobSql = $"""

                        UPDATE [{options.SchemaName}].[{options.JobsTableName}]
                        SET NextDueTime = @NextDueTime
                        WHERE Id = @JobId;
            """;

            var updateCommand = new CommandDefinition(
                updateJobSql,
                jobsToUpdate,
                transaction,
                cancellationToken: cancellationToken);
            await transaction.Connection.ExecuteAsync(updateCommand).ConfigureAwait(false);

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return dueJobs.Count;
        }
        catch (LostLeaseException)
        {
            await TryRollbackAsync(transaction, cancellationToken).ConfigureAwait(false);
            throw;
        }
        catch
        {
            await TryRollbackAsync(transaction, cancellationToken).ConfigureAwait(false);
            throw;
        }
        finally
        {
            await transaction.DisposeAsync().ConfigureAwait(false);
        }
    }

    public async Task<IReadOnlyList<(Guid Id, string Topic, string Payload)>> ClaimDueTimersAsync(
        ISystemLease lease,
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var claimCommand = new CommandDefinition(
            claimTimersSql,
            new { InstanceId = instanceId, FencingToken = lease.FencingToken, BatchSize = batchSize },
            cancellationToken: cancellationToken);
        var dueTimers = await connection.QueryAsync<(Guid Id, string Topic, string Payload)>(claimCommand).ConfigureAwait(false);

        return dueTimers.ToList();
    }

    public async Task<IReadOnlyList<(Guid Id, Guid JobId, string Topic, string Payload)>> ClaimDueJobRunsAsync(
        ISystemLease lease,
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var claimCommand = new CommandDefinition(
            claimJobsSql,
            new { InstanceId = instanceId, FencingToken = lease.FencingToken, BatchSize = batchSize },
            cancellationToken: cancellationToken);
        var dueJobs = await connection.QueryAsync<(Guid Id, Guid JobId, string Topic, string Payload)>(claimCommand).ConfigureAwait(false);

        return dueJobs.ToList();
    }

    public async Task UpdateSchedulerStateAsync(ISystemLease lease, CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var command = new CommandDefinition(
            schedulerStateUpdateSql,
            new
            {
                FencingToken = lease.FencingToken,
                LastRunAt = timeProvider.GetUtcNow(),
            },
            cancellationToken: cancellationToken);
        await connection.ExecuteAsync(command).ConfigureAwait(false);
    }

    private static async Task TryRollbackAsync(DbTransaction transaction, CancellationToken cancellationToken)
    {
        try
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            // Preserve the original processing exception when rollback fails.
        }
    }
}
