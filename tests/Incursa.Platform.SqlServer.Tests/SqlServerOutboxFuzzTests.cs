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
using Incursa.Platform.Outbox;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Incursa.Platform.Tests;

[Collection(SqlServerCollection.Name)]
[Trait("Category", "Integration")]
[Trait("RequiresDocker", "true")]
public sealed class SqlServerOutboxFuzzTests : SqlServerTestBase
{
    private sealed class DeterministicSequence(uint seed)
    {
        private uint state = seed;

        public int NextInt(int minInclusive, int maxExclusive)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(minInclusive, maxExclusive);
            state = (state * 1664525U) + 1013904223U;
            return minInclusive + (int)(state % (uint)(maxExclusive - minInclusive));
        }
    }

    private SqlOutboxService? outboxService;

    public SqlServerOutboxFuzzTests(ITestOutputHelper testOutputHelper, SqlServerCollectionFixture fixture)
        : base(testOutputHelper, fixture)
    {
    }

    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync().ConfigureAwait(false);
        await DatabaseSchemaManager.EnsureWorkQueueSchemaAsync(ConnectionString).ConfigureAwait(false);

        var options = Options.Create(new SqlOutboxOptions
        {
            ConnectionString = ConnectionString,
            SchemaName = "infra",
            TableName = "Outbox",
        });

        outboxService = new SqlOutboxService(options, NullLogger<SqlOutboxService>.Instance);
    }

    /// <summary>When deterministic randomized outbox operations run, then terminal items are never reclaimed.</summary>
    /// <intent>Use fixed-seed fuzzing to verify claim/ack/abandon/fail invariants under varied operation sequences.</intent>
    /// <scenario>Given 40 ready items and 40 deterministic random operation steps.</scenario>
    /// <behavior>Then completed/failed terminal items are never returned by subsequent claims.</behavior>
    [Fact]
    public async Task Outbox_FuzzDeterministic_TerminalItemsAreNeverReclaimed()
    {
        var seeded = new DeterministicSequence(1337U);
        var ownerToken = OwnerToken.GenerateNew();
        var terminal = new HashSet<OutboxWorkItemIdentifier>();

        await CreateTestOutboxItemsAsync(40);

        for (var step = 0; step < 40; step++)
        {
            var batchSize = seeded.NextInt(1, 6);
            var claimed = await outboxService!.ClaimAsync(ownerToken, leaseSeconds: 30, batchSize, TestContext.Current.CancellationToken);
            if (claimed.Count == 0)
            {
                break;
            }

            var operation = seeded.NextInt(0, 3);
            switch (operation)
            {
                case 0:
                    await outboxService.AckAsync(ownerToken, claimed, TestContext.Current.CancellationToken);
                    terminal.UnionWith(claimed);
                    break;
                case 1:
                    await outboxService.AbandonAsync(ownerToken, claimed, TestContext.Current.CancellationToken);
                    break;
                default:
                    await outboxService.FailAsync(ownerToken, claimed, TestContext.Current.CancellationToken);
                    terminal.UnionWith(claimed);
                    break;
            }
        }

        await AssertTerminalMessagesCannotBeClaimedAsync(terminal);
    }

    /// <summary>When two workers perform deterministic randomized claim rounds, then claimed sets stay disjoint and terminal items stay terminal.</summary>
    /// <intent>Exercise queue state transitions under concurrent claimers and mixed ack/abandon/fail actions.</intent>
    /// <scenario>Given 60 ready items and 20 deterministic rounds with two workers per round.</scenario>
    /// <behavior>Then claims in each round do not overlap and terminal ids are never reclaimed later.</behavior>
    [Fact]
    public async Task Outbox_FuzzConcurrentRounds_ClaimsStayDisjointAndTerminalItemsAreNotReclaimed()
    {
        var seeded = new DeterministicSequence(4242U);
        var terminal = new HashSet<OutboxWorkItemIdentifier>();

        await CreateTestOutboxItemsAsync(60);

        for (var round = 0; round < 20; round++)
        {
            var ownerA = OwnerToken.GenerateNew();
            var ownerB = OwnerToken.GenerateNew();

            var batchA = seeded.NextInt(1, 8);
            var batchB = seeded.NextInt(1, 8);

            var claimTaskA = outboxService!.ClaimAsync(ownerA, leaseSeconds: 30, batchA, TestContext.Current.CancellationToken);
            var claimTaskB = outboxService.ClaimAsync(ownerB, leaseSeconds: 30, batchB, TestContext.Current.CancellationToken);

            var claimed = await Task.WhenAll(claimTaskA, claimTaskB);
            var claimedA = claimed[0];
            var claimedB = claimed[1];

            claimedA.Intersect(claimedB).ShouldBeEmpty();

            await ApplyRandomOperationAsync(ownerA, claimedA, seeded, terminal);
            await ApplyRandomOperationAsync(ownerB, claimedB, seeded, terminal);
        }

        await AssertTerminalMessagesCannotBeClaimedAsync(terminal);
    }

    private async Task ApplyRandomOperationAsync(
        OwnerToken owner,
        IReadOnlyList<OutboxWorkItemIdentifier> claimed,
        DeterministicSequence seeded,
        ISet<OutboxWorkItemIdentifier> terminal)
    {
        if (claimed.Count == 0)
        {
            return;
        }

        var operation = seeded.NextInt(0, 3);
        switch (operation)
        {
            case 0:
                await outboxService!.AckAsync(owner, claimed, TestContext.Current.CancellationToken).ConfigureAwait(false);
                terminal.UnionWith(claimed);
                break;
            case 1:
                await outboxService!.AbandonAsync(owner, claimed, TestContext.Current.CancellationToken);
                break;
            default:
                await outboxService!.FailAsync(owner, claimed, TestContext.Current.CancellationToken);
                terminal.UnionWith(claimed);
                break;
        }
    }

    private async Task AssertTerminalMessagesCannotBeClaimedAsync(ISet<OutboxWorkItemIdentifier> terminal)
    {
        for (var scan = 0; scan < 10; scan++)
        {
            var scanOwner = OwnerToken.GenerateNew();
            var claimed = await outboxService!.ClaimAsync(scanOwner, leaseSeconds: 30, batchSize: 25, TestContext.Current.CancellationToken).ConfigureAwait(false);
            claimed.Intersect(terminal).ShouldBeEmpty();

            if (claimed.Count == 0)
            {
                break;
            }

            await outboxService.AckAsync(scanOwner, claimed, TestContext.Current.CancellationToken).ConfigureAwait(false);
        }
    }

    private async Task CreateTestOutboxItemsAsync(int count)
    {
        var connection = new SqlConnection(ConnectionString);
        await using (connection.ConfigureAwait(false))
        {
            await connection.OpenAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);

            for (var i = 0; i < count; i++)
            {
                await connection.ExecuteAsync(
                    @"
                INSERT INTO infra.Outbox (Id, Topic, Payload, Status, CreatedAt)
                VALUES (@Id, @Topic, @Payload, 0, SYSUTCDATETIME())",
                    new
                    {
                        Id = OutboxWorkItemIdentifier.GenerateNew(),
                        Topic = "fuzz",
                        Payload = $"payload{i}",
                    }).ConfigureAwait(false);
            }
        }
    }
}
