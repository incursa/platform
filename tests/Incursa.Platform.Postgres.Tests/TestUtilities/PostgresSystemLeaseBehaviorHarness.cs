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
using Npgsql;

namespace Incursa.Platform.Tests.TestUtilities;

internal sealed class PostgresSystemLeaseBehaviorHarness : PostgresTestBase, ISystemLeaseBehaviorHarness
{
    private PostgresLeaseFactory? leaseFactory;

    public PostgresSystemLeaseBehaviorHarness(ITestOutputHelper testOutputHelper, PostgresCollectionFixture fixture)
        : base(testOutputHelper, fixture)
    {
    }

    public ISystemLeaseFactory LeaseFactory => leaseFactory ?? throw new InvalidOperationException("Harness has not been initialized.");

    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync().ConfigureAwait(false);
        await DatabaseSchemaManager.EnsureDistributedLockSchemaAsync(ConnectionString, "infra", "DistributedLock").ConfigureAwait(false);

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
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);
        await connection.ExecuteAsync("""DELETE FROM "infra"."DistributedLock";""").ConfigureAwait(false);
    }
}
