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
using Shouldly;
using Xunit;

namespace Incursa.Platform.Tests.TestUtilities;

[SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Test naming uses underscores for readability.")]
public abstract class SystemLeaseBehaviorTestsBase : IAsyncLifetime
{
    private readonly ISystemLeaseBehaviorHarness harness;

    protected SystemLeaseBehaviorTestsBase(ISystemLeaseBehaviorHarness harness)
    {
        this.harness = harness ?? throw new ArgumentNullException(nameof(harness));
    }

    protected ISystemLeaseBehaviorHarness Harness => harness;

    public ValueTask InitializeAsync() => harness.InitializeAsync();

    public async ValueTask DisposeAsync()
    {
        await harness.DisposeAsync().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task AcquireAsync_WithFreeResource_ReturnsLease()
    {
        await harness.ResetAsync();

        await using var lease = await harness.LeaseFactory.AcquireAsync(
            "lease-free",
            TimeSpan.FromSeconds(30),
            cancellationToken: CancellationToken.None);

        lease.ShouldNotBeNull();
        lease.ResourceName.ShouldBe("lease-free");
    }

    [Fact]
    public async Task AcquireAsync_WithCustomOwnerToken_UsesProvidedToken()
    {
        await harness.ResetAsync();
        var expectedOwner = OwnerToken.GenerateNew();

        await using var lease = await harness.LeaseFactory.AcquireAsync(
            "lease-custom-owner",
            TimeSpan.FromSeconds(30),
            ownerToken: expectedOwner,
            cancellationToken: CancellationToken.None);

        lease.ShouldNotBeNull();
        lease.OwnerToken.ShouldBe(expectedOwner);
    }

    [Fact]
    public async Task AcquireAsync_WhenOccupied_ReturnsNull()
    {
        await harness.ResetAsync();

        await using var firstLease = await harness.LeaseFactory.AcquireAsync(
            "lease-occupied",
            TimeSpan.FromSeconds(30),
            cancellationToken: CancellationToken.None);

        firstLease.ShouldNotBeNull();

        await using var secondLease = await harness.LeaseFactory.AcquireAsync(
            "lease-occupied",
            TimeSpan.FromSeconds(30),
            cancellationToken: CancellationToken.None);

        secondLease.ShouldBeNull();
    }

    [Fact]
    public async Task TryRenewNowAsync_WithValidLease_ReturnsTrueAndIncrementsFencingToken()
    {
        await harness.ResetAsync();

        await using var lease = await harness.LeaseFactory.AcquireAsync(
            "lease-renew",
            TimeSpan.FromSeconds(30),
            cancellationToken: CancellationToken.None);

        lease.ShouldNotBeNull();
        var initialToken = lease.FencingToken;

        var renewed = await lease.TryRenewNowAsync(CancellationToken.None);

        renewed.ShouldBeTrue();
        lease.FencingToken.ShouldBeGreaterThan(initialToken);
    }

    [Fact]
    public async Task LeaseLoss_AfterExpiry_CancelsToken_AndThrowIfLostThrows()
    {
        await harness.ResetAsync();

        await using var lease = await harness.LeaseFactory.AcquireAsync(
            "lease-expiry-loss",
            TimeSpan.FromSeconds(1),
            cancellationToken: CancellationToken.None);

        lease.ShouldNotBeNull();

        var timeoutAt = DateTimeOffset.UtcNow.AddSeconds(5);
        while (!lease.CancellationToken.IsCancellationRequested && DateTimeOffset.UtcNow < timeoutAt)
        {
            await Task.Delay(100, CancellationToken.None);
        }

        lease.CancellationToken.IsCancellationRequested.ShouldBeTrue();
        Should.Throw<LostLeaseException>(() => lease.ThrowIfLost());
    }

    [Fact]
    public async Task TryRenewNowAsync_AfterLeaseLoss_ReturnsFalse()
    {
        await harness.ResetAsync();

        await using var lease = await harness.LeaseFactory.AcquireAsync(
            "lease-expiry-renew",
            TimeSpan.FromSeconds(1),
            cancellationToken: CancellationToken.None);

        lease.ShouldNotBeNull();

        await Task.Delay(TimeSpan.FromMilliseconds(1300), CancellationToken.None);
        var renewed = await lease.TryRenewNowAsync(CancellationToken.None);

        renewed.ShouldBeFalse();
    }

    [Fact]
    public async Task DisposeAsync_ReleasesLease_ForReacquisition()
    {
        await harness.ResetAsync();

        var lease = await harness.LeaseFactory.AcquireAsync(
            "lease-dispose",
            TimeSpan.FromSeconds(30),
            cancellationToken: CancellationToken.None);

        lease.ShouldNotBeNull();
        await lease.DisposeAsync();

        await using var reacquired = await harness.LeaseFactory.AcquireAsync(
            "lease-dispose",
            TimeSpan.FromSeconds(30),
            cancellationToken: CancellationToken.None);

        reacquired.ShouldNotBeNull();
    }

    [Fact]
    public async Task AcquireAsync_AfterExpiry_ReturnsNewLease()
    {
        await harness.ResetAsync();

        await using var firstLease = await harness.LeaseFactory.AcquireAsync(
            "lease-expiry-reacquire",
            TimeSpan.FromSeconds(1),
            cancellationToken: CancellationToken.None);

        firstLease.ShouldNotBeNull();

        await Task.Delay(TimeSpan.FromMilliseconds(1300), CancellationToken.None);

        await using var secondLease = await harness.LeaseFactory.AcquireAsync(
            "lease-expiry-reacquire",
            TimeSpan.FromSeconds(30),
            cancellationToken: CancellationToken.None);

        secondLease.ShouldNotBeNull();
    }
}
