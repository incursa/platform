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

using Dapper;
using Incursa.Platform.Tests.TestUtilities;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Incursa.Platform.Tests.TestUtilities;

internal sealed class PostgresSchedulerBehaviorHarness : PostgresTestBase, ISchedulerBehaviorHarness
{
    private readonly PostgresSchedulerOptions options = new()
    {
        ConnectionString = string.Empty,
        SchemaName = "infra",
        JobsTableName = "Jobs",
        JobRunsTableName = "JobRuns",
        TimersTableName = "Timers",
    };

    private PostgresSchedulerClient? schedulerClient;
    private PostgresSchedulerStore? schedulerStore;
    private PostgresLeaseFactory? leaseFactory;

    public PostgresSchedulerBehaviorHarness(ITestOutputHelper testOutputHelper, PostgresCollectionFixture fixture)
        : base(testOutputHelper, fixture)
    {
    }

    public ISchedulerClient SchedulerClient => schedulerClient ?? throw new InvalidOperationException("Harness has not been initialized.");

    public ISchedulerStore SchedulerStore => schedulerStore ?? throw new InvalidOperationException("Harness has not been initialized.");

    public ISystemLeaseFactory LeaseFactory => leaseFactory ?? throw new InvalidOperationException("Harness has not been initialized.");

    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync().ConfigureAwait(false);

        await DatabaseSchemaManager.EnsureSchedulerSchemaAsync(ConnectionString, "infra", "Jobs", "JobRuns", "Timers").ConfigureAwait(false);
        await DatabaseSchemaManager.EnsureDistributedLockSchemaAsync(ConnectionString, "infra", "DistributedLock").ConfigureAwait(false);

        options.ConnectionString = ConnectionString;
        schedulerClient = new PostgresSchedulerClient(Options.Create(options), TimeProvider.System);
        schedulerStore = new PostgresSchedulerStore(Options.Create(options), TimeProvider.System);
        leaseFactory = new PostgresLeaseFactory(
            new LeaseFactoryConfig
            {
                ConnectionString = ConnectionString,
                SchemaName = "infra",
                RenewPercent = 2.0,
                UseGate = false,
                GateTimeoutMs = 200,
            },
            NullLogger<PostgresLeaseFactory>.Instance);
    }

    public async Task ResetAsync()
    {
        var connection = new NpgsqlConnection(ConnectionString);
        await using (connection.ConfigureAwait(false))
        {
            await connection.OpenAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);

        const string sql = """
            DELETE FROM "infra"."JobRuns";
            DELETE FROM "infra"."Timers";
            DELETE FROM "infra"."Jobs";
            DELETE FROM "infra"."DistributedLock";

            INSERT INTO "infra"."SchedulerState" ("Id", "CurrentFencingToken", "LastRunAt")
            VALUES (1, 0, NULL)
            ON CONFLICT ("Id") DO UPDATE
            SET "CurrentFencingToken" = 0,
                "LastRunAt" = NULL;
            """;

        await connection.ExecuteAsync(sql).ConfigureAwait(false);
        }
    }
}
