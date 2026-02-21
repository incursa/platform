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

using System.Linq;
using Incursa.Platform.Outbox;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Incursa.Platform.Tests;

[Trait("Category", "Unit")]
public sealed class InMemoryOutboxFuzzTests
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

    /// <summary>When outbox Store Fuzz Deterministic Terminal Items Are Never Reclaimed, then it behaves as expected.</summary>
    /// <intent>Document expected behavior for outbox Store Fuzz Deterministic Terminal Items Are Never Reclaimed.</intent>
    /// <scenario>Given outbox Store Fuzz Deterministic Terminal Items Are Never Reclaimed.</scenario>
    /// <behavior>Then the operation matches the expected outcome.</behavior>
    [Fact]
    public async Task OutboxStore_FuzzDeterministic_TerminalItemsAreNeverReclaimed()
    {
        var seeded = new DeterministicSequence(7331U);
        using var provider = BuildProvider();

        var outbox = provider.GetRequiredService<IOutbox>();
        var stores = await provider.GetRequiredService<IOutboxStoreProvider>().GetAllStoresAsync();
        var store = stores.Single();

        var terminal = new HashSet<OutboxWorkItemIdentifier>();

        for (var i = 0; i < 40; i++)
        {
            await outbox.EnqueueAsync($"topic.{i}", $"payload.{i}", TestContext.Current.CancellationToken);
        }

        for (var step = 0; step < 40; step++)
        {
            var batchSize = seeded.NextInt(1, 6);
            var claimed = await store.ClaimDueAsync(batchSize, TestContext.Current.CancellationToken);
            if (claimed.Count == 0)
            {
                break;
            }

            foreach (var message in claimed)
            {
                var operation = seeded.NextInt(0, 3);
                switch (operation)
                {
                    case 0:
                        await store.MarkDispatchedAsync(message.Id, TestContext.Current.CancellationToken);
                        terminal.Add(message.Id);
                        break;
                    case 1:
                        await store.RescheduleAsync(message.Id, TimeSpan.Zero, "fuzz-reschedule", TestContext.Current.CancellationToken);
                        break;
                    default:
                        await store.FailAsync(message.Id, "fuzz-fail", TestContext.Current.CancellationToken);
                        terminal.Add(message.Id);
                        break;
                }
            }
        }

        for (var scan = 0; scan < 10; scan++)
        {
            var claimed = await store.ClaimDueAsync(25, TestContext.Current.CancellationToken);
            claimed.Select(m => m.Id).Intersect(terminal).ShouldBeEmpty();

            if (claimed.Count == 0)
            {
                break;
            }

            foreach (var message in claimed)
            {
                await store.MarkDispatchedAsync(message.Id, TestContext.Current.CancellationToken);
            }
        }
    }

    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddInMemoryPlatformMultiDatabaseWithList(new[]
        {
            new InMemoryPlatformDatabase { Name = "default" },
        });

        return services.BuildServiceProvider();
    }
}
