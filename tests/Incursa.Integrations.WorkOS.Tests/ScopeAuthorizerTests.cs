namespace Incursa.Integrations.WorkOS.Tests;

using Incursa.Integrations.WorkOS.Abstractions.Authentication;
using Incursa.Integrations.WorkOS.Core.Authorization;

[TestClass]
public sealed class ScopeAuthorizerTests
{
    [TestMethod]
    public async Task AuthorizeAsync_ArtifactsReadAlias_AllowsNuGetRead()
    {
        var sut = new WorkOsScopeAuthorizer();
        var identity = CreateIdentity("tenant-a", "nuget.read");

        var result = await sut.AuthorizeAsync(identity, "artifacts:nuget:read", "tenant-a").ConfigureAwait(false);

        Assert.IsTrue(result.IsValid);
    }

    [TestMethod]
    public async Task AuthorizeAsync_ArtifactsWriteAlias_AllowsAdmin()
    {
        var sut = new WorkOsScopeAuthorizer();
        var identity = CreateIdentity("tenant-a", "admin");

        var result = await sut.AuthorizeAsync(identity, "artifacts:raw:push", "tenant-a").ConfigureAwait(false);

        Assert.IsTrue(result.IsValid);
    }

    [TestMethod]
    public async Task AuthorizeAsync_TenantMismatch_Denies()
    {
        var sut = new WorkOsScopeAuthorizer();
        var identity = CreateIdentity("tenant-a", "nuget.read");

        var result = await sut.AuthorizeAsync(identity, "artifacts:nuget:read", "tenant-b").ConfigureAwait(false);

        Assert.IsFalse(result.IsValid);
        Assert.AreEqual(WorkOsValidationErrorCode.UnknownOrganization, result.ErrorCode);
    }

    private static WorkOsAuthIdentity CreateIdentity(string tenantId, params string[] scopes)
    {
        return new WorkOsAuthIdentity(
            Subject: "api_key:key_1",
            ApiKeyId: "key_1",
            OrganizationId: "org_1",
            TenantId: tenantId,
            Scopes: scopes,
            CreatedUtc: DateTimeOffset.UtcNow.AddMinutes(-5),
            ExpiresUtc: DateTimeOffset.UtcNow.AddHours(1),
            RevokedUtc: null,
            DisplayName: "Key 1");
    }
}
