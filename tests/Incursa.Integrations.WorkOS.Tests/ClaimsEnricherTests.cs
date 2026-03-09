namespace Incursa.Integrations.WorkOS.Tests;

using System.Security.Claims;
using Incursa.Integrations.WorkOS.Abstractions.Claims;
using Incursa.Integrations.WorkOS.Abstractions.Configuration;
using Incursa.Integrations.WorkOS.Abstractions.Profiles;
using Incursa.Integrations.WorkOS.Core.Claims;
using Incursa.Integrations.WorkOS.Core.Profiles;

[TestClass]
public sealed class ClaimsEnricherTests
{
    [TestMethod]
    public async Task EnrichAsync_MissingClaims_AddsMembershipAndPermissionClaims()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "user_123")], "test"));
        var identity = (ClaimsIdentity)principal.Identity!;
        var enricher = new WorkOsClaimsEnricher(
            new FakeMembershipClient(),
            new WorkOsClaimsEnrichmentOptions());

        await enricher.EnrichAsync(principal, identity);

        Assert.IsTrue(identity.HasClaim("org_id", "org_123"));
        Assert.IsTrue(identity.HasClaim("workos:role", "admin"));
        Assert.IsTrue(identity.HasClaim("workos:permission", "conduit:admin"));
    }

    [TestMethod]
    public async Task EnrichAsync_ApiFallbackDisabled_DoesNothing()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "user_123")], "test"));
        var identity = (ClaimsIdentity)principal.Identity!;
        var enricher = new WorkOsClaimsEnricher(
            new FakeMembershipClient(),
            new WorkOsClaimsEnrichmentOptions
            {
                EnableApiFallback = false,
            });

        await enricher.EnrichAsync(principal, identity);

        Assert.IsFalse(identity.HasClaim("org_id", "org_123"));
    }

    [TestMethod]
    public async Task EnrichAsync_ProfileProviderPath_ProjectsClaims()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "user_123")], "test"));
        var identity = (ClaimsIdentity)principal.Identity!;
        var enricher = new WorkOsClaimsEnricher(
            new FakeProfileProvider(),
            new WorkOsUserProfileProjector(
                new WorkOsClaimsEnrichmentOptions(),
                new WorkOsUserProfileHydrationOptions()),
            new WorkOsClaimsEnrichmentOptions());

        await enricher.EnrichAsync(principal, identity);

        Assert.IsTrue(identity.HasClaim("org_id", "org_123"));
        Assert.IsTrue(identity.HasClaim("workos:role", "admin"));
        Assert.IsTrue(identity.HasClaim("workos:permission", "conduit:admin"));
    }

    private sealed class FakeMembershipClient : IWorkOsMembershipClient
    {
        public ValueTask<IReadOnlyCollection<WorkOsOrganizationMembershipInfo>> ListOrganizationMembershipsAsync(string userId, CancellationToken ct = default)
            => ValueTask.FromResult<IReadOnlyCollection<WorkOsOrganizationMembershipInfo>>(
                [new WorkOsOrganizationMembershipInfo("org_123", ["admin"])]);

        public ValueTask<IReadOnlyCollection<string>> ListRolePermissionsAsync(string organizationId, IEnumerable<string> roleSlugs, CancellationToken ct = default)
            => ValueTask.FromResult<IReadOnlyCollection<string>>(["conduit:admin"]);
    }

    private sealed class FakeProfileProvider : IWorkOsUserProfileProvider
    {
        public ValueTask<WorkOsUserProfile?> GetProfileAsync(ClaimsPrincipal principal, CancellationToken ct = default)
            => ValueTask.FromResult<WorkOsUserProfile?>(
                new WorkOsUserProfile(
                    Subject: "user_123",
                    OrganizationIds: ["org_123"],
                    RolesByOrganization: new Dictionary<string, IReadOnlyCollection<string>>(StringComparer.Ordinal)
                    {
                        ["org_123"] = ["admin"],
                    },
                    Permissions: ["conduit:admin"],
                    Metadata: new Dictionary<string, string>(StringComparer.Ordinal),
                    HydratedUtc: DateTimeOffset.UtcNow));
    }
}
