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

using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Incursa.Platform.Tests;

[Trait("Category", "Unit")]
public sealed class InMemoryPublicApiContractTests
{
    [Fact]
    public void AddInMemoryPlatformMultiDatabaseWithList_WithNullDatabases_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();

        Should.Throw<ArgumentNullException>(() =>
            services.AddInMemoryPlatformMultiDatabaseWithList(null!));
    }

    [Fact]
    public void AddInMemoryPlatformMultiDatabaseWithList_WithEmptyDatabases_ThrowsArgumentException()
    {
        var services = new ServiceCollection();

        Should.Throw<ArgumentException>(() =>
            services.AddInMemoryPlatformMultiDatabaseWithList(Array.Empty<InMemoryPlatformDatabase>()));
    }

    [Fact]
    public void AddInMemoryPlatformMultiDatabaseWithList_RegistersCorePrimitiveServices()
    {
        var services = new ServiceCollection();
        services.AddInMemoryPlatformMultiDatabaseWithList(new[]
        {
            new InMemoryPlatformDatabase { Name = "default" },
        });

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<IOutbox>().ShouldNotBeNull();
        provider.GetRequiredService<IInbox>().ShouldNotBeNull();
        provider.GetRequiredService<ISchedulerClient>().ShouldNotBeNull();
        provider.GetRequiredService<ISystemLeaseFactory>().ShouldNotBeNull();
    }

    [Fact]
    public void AddFanoutTopic_WithMissingFanoutTopic_ThrowsArgumentException()
    {
        var services = new ServiceCollection();
        var options = new FanoutTopicOptions
        {
            FanoutTopic = string.Empty,
        };

        Should.Throw<ArgumentException>(() => services.AddFanoutTopic<TestFanoutPlanner>(options));
    }

    [Fact]
    public void InMemoryOptions_HaveExpectedDefaults()
    {
        var outbox = new InMemoryOutboxOptions { StoreKey = "default" };
        var inbox = new InMemoryInboxOptions { StoreKey = "default" };
        var scheduler = new InMemorySchedulerOptions { StoreKey = "default" };
        var fanout = new InMemoryFanoutOptions { StoreKey = "default" };
        var platform = new InMemoryPlatformOptions();

        outbox.RetentionPeriod.ShouldBe(TimeSpan.FromDays(7));
        outbox.EnableAutomaticCleanup.ShouldBeTrue();
        outbox.CleanupInterval.ShouldBe(TimeSpan.FromHours(1));
        outbox.LeaseDuration.ShouldBe(TimeSpan.FromMinutes(5));

        inbox.RetentionPeriod.ShouldBe(TimeSpan.FromDays(7));
        inbox.EnableAutomaticCleanup.ShouldBeTrue();
        inbox.CleanupInterval.ShouldBe(TimeSpan.FromHours(1));

        scheduler.StoreKey.ShouldBe("default");
        fanout.StoreKey.ShouldBe("default");
        platform.EnableSchedulerWorkers.ShouldBeTrue();
    }

    private sealed class TestFanoutPlanner : IFanoutPlanner
    {
        public Task<IReadOnlyList<FanoutSlice>> GetDueSlicesAsync(string fanoutTopic, string? workKey, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<FanoutSlice>>(Array.Empty<FanoutSlice>());
    }
}
