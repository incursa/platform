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
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Incursa.Platform.Tests;

[Trait("Category", "Unit")]
public sealed class InMemoryGeneralCharacteristicsTests
{
    /// <summary>When independent providers are used, then in-memory state stays process-local and non-durable.</summary>
    /// <intent>Verify outbox state is isolated per provider instance and does not persist across provider recreation.</intent>
    /// <scenario>Given one provider with an enqueued outbox message and separate provider instances using the same logical database name.</scenario>
    /// <behavior>Then only the original provider can claim the message, and a newly created provider has no prior state.</behavior>
    [Fact]
    public async Task ProcessLocalState_IsIsolatedAcrossProviderInstances_AndNotDurable()
    {
        using var providerA = BuildProvider();
        var outboxA = providerA.GetRequiredService<IOutbox>();
        var storeProviderA = providerA.GetRequiredService<IOutboxStoreProvider>();
        var storeA = (await storeProviderA.GetAllStoresAsync()).Single();

        await outboxA.EnqueueAsync("test.topic", "payload", TestContext.Current.CancellationToken);

        using var providerB = BuildProvider();
        var storeProviderB = providerB.GetRequiredService<IOutboxStoreProvider>();
        var storeB = (await storeProviderB.GetAllStoresAsync()).Single();
        var claimedFromB = await storeB.ClaimDueAsync(10, TestContext.Current.CancellationToken);
        claimedFromB.Count.ShouldBe(0);

        var claimedFromA = await storeA.ClaimDueAsync(10, TestContext.Current.CancellationToken);
        claimedFromA.Count.ShouldBe(1);

        using var providerC = BuildProvider();
        var storeProviderC = providerC.GetRequiredService<IOutboxStoreProvider>();
        var storeC = (await storeProviderC.GetAllStoresAsync()).Single();
        var claimedFromC = await storeC.ClaimDueAsync(10, TestContext.Current.CancellationToken);
        claimedFromC.Count.ShouldBe(0);
    }

    /// <summary>When independent providers acquire the same lease resource, then both acquisitions succeed.</summary>
    /// <intent>Verify the in-memory provider does not provide cross-provider distributed coordination guarantees.</intent>
    /// <scenario>Given two separate provider instances each requesting the same lease resource name.</scenario>
    /// <behavior>Then each provider acquires its own lease for the same resource.</behavior>
    [Fact]
    public async Task LeaseCoordination_IsNotSharedAcrossIndependentProviderInstances()
    {
        using var providerA = BuildProvider();
        using var providerB = BuildProvider();

        var leaseFactoryA = providerA.GetRequiredService<ISystemLeaseFactory>();
        var leaseFactoryB = providerB.GetRequiredService<ISystemLeaseFactory>();

        await using var leaseA = await leaseFactoryA.AcquireAsync(
            resourceName: "resource-a",
            leaseDuration: TimeSpan.FromSeconds(30),
            cancellationToken: TestContext.Current.CancellationToken);
        await using var leaseB = await leaseFactoryB.AcquireAsync(
            resourceName: "resource-a",
            leaseDuration: TimeSpan.FromSeconds(30),
            cancellationToken: TestContext.Current.CancellationToken);

        leaseA.ShouldNotBeNull();
        leaseB.ShouldNotBeNull();
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
