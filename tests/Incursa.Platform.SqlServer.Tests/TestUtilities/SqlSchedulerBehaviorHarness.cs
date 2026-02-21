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
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Incursa.Platform.Tests.TestUtilities;

internal sealed class SqlSchedulerBehaviorHarness : SqlServerTestBase, ISchedulerBehaviorHarness
{
    private readonly SqlSchedulerOptions options = new()
    {
        ConnectionString = string.Empty,
        SchemaName = "infra",
        JobsTableName = "Jobs",
        JobRunsTableName = "JobRuns",
        TimersTableName = "Timers",
    };

    private SqlSchedulerClient? schedulerClient;
    private SqlSchedulerStore? schedulerStore;
    private SqlLeaseFactory? leaseFactory;

    public SqlSchedulerBehaviorHarness(ITestOutputHelper testOutputHelper, SqlServerCollectionFixture fixture)
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
        schedulerClient = new SqlSchedulerClient(Options.Create(options), TimeProvider.System);
        schedulerStore = new SqlSchedulerStore(Options.Create(options), TimeProvider.System);
        leaseFactory = new SqlLeaseFactory(
            new LeaseFactoryConfig
            {
                ConnectionString = ConnectionString,
                SchemaName = "infra",
                RenewPercent = 2.0,
                UseGate = false,
                GateTimeoutMs = 200,
            },
            NullLogger<SqlLeaseFactory>.Instance);
    }

    public async Task ResetAsync()
    {
        var connection = new SqlConnection(ConnectionString);
        await using (connection.ConfigureAwait(false))
        {
            await connection.OpenAsync(TestContext.Current.CancellationToken);

            const string sql = """
            DELETE FROM [infra].[JobRuns];
            DELETE FROM [infra].[Timers];
            DELETE FROM [infra].[Jobs];
            DELETE FROM [infra].[DistributedLock];

            UPDATE [infra].[SchedulerState]
            SET [CurrentFencingToken] = 0,
                [LastRunAt] = NULL
            WHERE [Id] = 1;

            IF @@ROWCOUNT = 0
            BEGIN
                INSERT INTO [infra].[SchedulerState] ([Id], [CurrentFencingToken], [LastRunAt])
                VALUES (1, 0, NULL);
            END
            """;

            await connection.ExecuteAsync(sql).ConfigureAwait(false);
        }
    }
}
