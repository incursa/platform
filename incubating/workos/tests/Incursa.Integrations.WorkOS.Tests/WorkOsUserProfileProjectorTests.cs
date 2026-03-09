namespace Incursa.Integrations.WorkOS.Tests;

using System.Security.Claims;
using Incursa.Integrations.WorkOS.Abstractions.Configuration;
using Incursa.Integrations.WorkOS.Abstractions.Profiles;
using Incursa.Integrations.WorkOS.Core.Profiles;

[TestClass]
public sealed class WorkOsUserProfileProjectorTests
{
    [TestMethod]
    public void ProjectToClaims_ReplacesPriorWorkOsClaims_AndAddsProfileClaims()
    {
        var identity = new ClaimsIdentity(
        [
            new Claim("org_id", "org_old"),
            new Claim("workos:role", "viewer"),
            new Claim("workos:permission", "permission:old"),
            new Claim("workos:profile:locale", "en-US"),
        ], "test");

        var profile = new WorkOsUserProfile(
            Subject: "user_123",
            OrganizationIds: ["org_1", "org_2"],
            RolesByOrganization: new Dictionary<string, IReadOnlyCollection<string>>(StringComparer.Ordinal)
            {
                ["org_1"] = ["admin"],
                ["org_2"] = ["viewer"],
            },
            Permissions: ["conduit:admin", "conduit:viewer"],
            Metadata: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["locale"] = "fr-CA",
            },
            HydratedUtc: DateTimeOffset.UtcNow);

        var projector = new WorkOsUserProfileProjector(
            new WorkOsClaimsEnrichmentOptions(),
            new WorkOsUserProfileHydrationOptions());

        projector.ProjectToClaims(profile, identity);

        Assert.IsFalse(identity.HasClaim("org_id", "org_old"));
        Assert.IsFalse(identity.HasClaim("workos:permission", "permission:old"));
        Assert.IsFalse(identity.HasClaim("workos:profile:locale", "en-US"));
        Assert.IsTrue(identity.HasClaim("org_id", "org_1"));
        Assert.IsTrue(identity.HasClaim("org_id", "org_2"));
        Assert.IsTrue(identity.HasClaim("workos:role", "admin"));
        Assert.IsTrue(identity.HasClaim("workos:role", "viewer"));
        Assert.IsTrue(identity.HasClaim("workos:permission", "conduit:admin"));
        Assert.IsTrue(identity.HasClaim("workos:permission", "conduit:viewer"));
        Assert.IsTrue(identity.HasClaim("workos:profile:locale", "fr-CA"));
    }

    [TestMethod]
    public void ProjectToClaims_AddsRawProfileJson_WhenEnabled()
    {
        var identity = new ClaimsIdentity([], "test");
        var profile = new WorkOsUserProfile(
            Subject: "user_123",
            OrganizationIds: ["org_1"],
            RolesByOrganization: new Dictionary<string, IReadOnlyCollection<string>>(StringComparer.Ordinal)
            {
                ["org_1"] = ["admin"],
            },
            Permissions: ["conduit:admin"],
            Metadata: new Dictionary<string, string>(StringComparer.Ordinal),
            HydratedUtc: DateTimeOffset.UtcNow);

        var projector = new WorkOsUserProfileProjector(
            new WorkOsClaimsEnrichmentOptions(),
            new WorkOsUserProfileHydrationOptions
            {
                IncludeRawProfileJson = true,
            });

        projector.ProjectToClaims(profile, identity);

        Claim? raw = identity.FindFirst("workos:profile:raw_json");
        Assert.IsNotNull(raw);
        Assert.IsTrue(raw.Value.Contains("\"Subject\":\"user_123\"", StringComparison.Ordinal));
    }
}
