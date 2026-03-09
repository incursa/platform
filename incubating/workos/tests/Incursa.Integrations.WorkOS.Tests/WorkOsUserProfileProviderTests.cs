namespace Incursa.Integrations.WorkOS.Tests;

using System.Security.Claims;
using Incursa.Integrations.WorkOS.Abstractions.Claims;
using Incursa.Integrations.WorkOS.Abstractions.Configuration;
using Incursa.Integrations.WorkOS.Abstractions.Profiles;
using Incursa.Integrations.WorkOS.Core.Profiles;
using Microsoft.Extensions.Caching.Memory;

[TestClass]
public sealed class WorkOsUserProfileProviderTests
{
    [TestMethod]
    public async Task GetProfileAsync_UsesCachedProfile_WhenFresh()
    {
        CountingMembershipClient.Reset();
        var cache = new MemoryWorkOsUserProfileCache(new MemoryCache(new MemoryCacheOptions()));
        var cached = new WorkOsUserProfile(
            Subject: "user_123",
            OrganizationIds: ["org_a"],
            RolesByOrganization: new Dictionary<string, IReadOnlyCollection<string>>(StringComparer.Ordinal)
            {
                ["org_a"] = ["admin"],
            },
            Permissions: ["permission:a"],
            Metadata: new Dictionary<string, string>(StringComparer.Ordinal),
            HydratedUtc: DateTimeOffset.UtcNow);

        await cache.SetAsync("user_123", cached).ConfigureAwait(false);

        var provider = new WorkOsUserProfileProvider(
            new CountingMembershipClient(),
            cache,
            new WorkOsUserProfileHydrationOptions
            {
                CacheTtl = TimeSpan.FromMinutes(10),
                RevalidateOnRequestIfStale = true,
            });

        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "user_123")], "test"));
        WorkOsUserProfile? profile = await provider.GetProfileAsync(principal).ConfigureAwait(false);

        Assert.IsNotNull(profile);
        Assert.AreEqual("user_123", profile.Subject);
        Assert.HasCount(1, profile.OrganizationIds);
        Assert.AreEqual(0, CountingMembershipClient.ListMembershipCalls);
    }

    [TestMethod]
    public async Task GetProfileAsync_Revalidates_WhenStaleAndEnabled()
    {
        CountingMembershipClient.Reset();
        var cache = new MemoryWorkOsUserProfileCache(new MemoryCache(new MemoryCacheOptions()));
        var stale = new WorkOsUserProfile(
            Subject: "user_123",
            OrganizationIds: ["org_stale"],
            RolesByOrganization: new Dictionary<string, IReadOnlyCollection<string>>(StringComparer.Ordinal)
            {
                ["org_stale"] = ["viewer"],
            },
            Permissions: ["permission:stale"],
            Metadata: new Dictionary<string, string>(StringComparer.Ordinal),
            HydratedUtc: DateTimeOffset.UtcNow.AddHours(-1));
        await cache.SetAsync("user_123", stale).ConfigureAwait(false);

        var membershipClient = new CountingMembershipClient();
        var provider = new WorkOsUserProfileProvider(
            membershipClient,
            cache,
            new WorkOsUserProfileHydrationOptions
            {
                CacheTtl = TimeSpan.FromMinutes(1),
                RevalidateOnRequestIfStale = true,
            });

        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "user_123")], "test"));
        WorkOsUserProfile? profile = await provider.GetProfileAsync(principal).ConfigureAwait(false);

        Assert.IsNotNull(profile);
        Assert.IsTrue(profile.OrganizationIds.Contains("org_123"));
        Assert.IsTrue(profile.Permissions.Contains("conduit:admin"));
        Assert.AreEqual(1, CountingMembershipClient.ListMembershipCalls);
        Assert.AreEqual(1, CountingMembershipClient.ListPermissionCalls);
    }

    [TestMethod]
    public async Task GetProfileAsync_ReturnsStale_WhenRevalidateDisabled()
    {
        CountingMembershipClient.Reset();
        var cache = new MemoryWorkOsUserProfileCache(new MemoryCache(new MemoryCacheOptions()));
        var stale = new WorkOsUserProfile(
            Subject: "user_123",
            OrganizationIds: ["org_cached"],
            RolesByOrganization: new Dictionary<string, IReadOnlyCollection<string>>(StringComparer.Ordinal)
            {
                ["org_cached"] = ["cached_role"],
            },
            Permissions: ["permission:cached"],
            Metadata: new Dictionary<string, string>(StringComparer.Ordinal),
            HydratedUtc: DateTimeOffset.UtcNow.AddHours(-1));
        await cache.SetAsync("user_123", stale).ConfigureAwait(false);

        var provider = new WorkOsUserProfileProvider(
            new CountingMembershipClient(),
            cache,
            new WorkOsUserProfileHydrationOptions
            {
                CacheTtl = TimeSpan.Zero,
                RevalidateOnRequestIfStale = false,
            });

        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "user_123")], "test"));
        WorkOsUserProfile? profile = await provider.GetProfileAsync(principal).ConfigureAwait(false);

        Assert.IsNotNull(profile);
        Assert.IsTrue(profile.OrganizationIds.Contains("org_cached"));
        Assert.AreEqual(0, CountingMembershipClient.ListMembershipCalls);
        Assert.AreEqual(0, CountingMembershipClient.ListPermissionCalls);
    }

    private sealed class CountingMembershipClient : IWorkOsMembershipClient
    {
        public static int ListMembershipCalls { get; private set; }

        public static int ListPermissionCalls { get; private set; }

        public static void Reset()
        {
            ListMembershipCalls = 0;
            ListPermissionCalls = 0;
        }

        public ValueTask<IReadOnlyCollection<WorkOsOrganizationMembershipInfo>> ListOrganizationMembershipsAsync(string userId, CancellationToken ct = default)
        {
            ListMembershipCalls++;
            return ValueTask.FromResult<IReadOnlyCollection<WorkOsOrganizationMembershipInfo>>(
                [
                    new WorkOsOrganizationMembershipInfo("org_123", ["admin", "admin"]),
                ]);
        }

        public ValueTask<IReadOnlyCollection<string>> ListRolePermissionsAsync(string organizationId, IEnumerable<string> roleSlugs, CancellationToken ct = default)
        {
            ListPermissionCalls++;
            return ValueTask.FromResult<IReadOnlyCollection<string>>(["conduit:admin", "conduit:admin"]);
        }
    }
}
