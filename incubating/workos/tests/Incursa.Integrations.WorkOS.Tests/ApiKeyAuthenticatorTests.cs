namespace Incursa.Integrations.WorkOS.Tests;

using Incursa.Integrations.WorkOS.Abstractions.Authentication;
using Incursa.Integrations.WorkOS.Abstractions.Configuration;
using Incursa.Integrations.WorkOS.Abstractions.Management;
using Incursa.Integrations.WorkOS.Abstractions.Mapping;
using Incursa.Integrations.WorkOS.Core.Authentication;
using Incursa.Integrations.WorkOS.Core.Authorization;
using Incursa.Integrations.WorkOS.Core.Clients;
using Microsoft.Extensions.Caching.Memory;

[TestClass]
public sealed class ApiKeyAuthenticatorTests
{
    [TestMethod]
    public async Task ValidateApiKeyAsync_UnknownOrgMapping_ReturnsUnknownOrganization()
    {
        var options = WorkOsPermissionMappingOptions.CreateDefaultArtifacts();
        var mapper = new WorkOsPermissionMapper(options);
        var auth = new WorkOsApiKeyAuthenticator(
            new FakeManagementClient(),
            new FakeTenantMapper(null),
            mapper,
            new MemoryCache(new MemoryCacheOptions()),
            new WorkOsIntegrationOptions());

        var result = await auth.ValidateApiKeyAsync("secret");

        Assert.IsFalse(result.IsValid);
        Assert.AreEqual(WorkOsValidationErrorCode.UnknownOrganization, result.ErrorCode);
    }

    [TestMethod]
    public async Task ValidateApiKeyAsync_Valid_ReturnsIdentity()
    {
        var options = WorkOsPermissionMappingOptions.CreateDefaultArtifacts();
        var mapper = new WorkOsPermissionMapper(options);
        var auth = new WorkOsApiKeyAuthenticator(
            new FakeManagementClient(),
            new FakeTenantMapper("tenant-a"),
            mapper,
            new MemoryCache(new MemoryCacheOptions()),
            new WorkOsIntegrationOptions());

        var result = await auth.ValidateApiKeyAsync("secret");

        Assert.IsTrue(result.IsValid);
        Assert.IsNotNull(result.Identity);
        Assert.AreEqual("tenant-a", result.Identity.TenantId);
        CollectionAssert.Contains(result.Identity.Scopes.ToArray(), "nuget.read");
    }

    private sealed class FakeManagementClient : IWorkOsManagementClient
    {
        public ValueTask<WorkOsManagedApiKey?> ValidateApiKeyAsync(string presentedKey, CancellationToken ct = default)
            => ValueTask.FromResult<WorkOsManagedApiKey?>(new WorkOsManagedApiKey(
                ApiKeyId: "key_123",
                OrganizationId: "org_123",
                DisplayName: "test",
                CreatedUtc: DateTimeOffset.UtcNow,
                ExpiresUtc: null,
                RevokedUtc: null,
                Permissions: ["nuget.read"]));

        public ValueTask<WorkOsCreatedApiKey> CreateApiKeyAsync(string organizationId, string displayName, IReadOnlyCollection<string> scopes, int? ttlHours, CancellationToken ct = default)
            => throw new NotSupportedException();

        public IAsyncEnumerable<WorkOsApiKeySummary> ListApiKeysAsync(string organizationId, CancellationToken ct = default)
            => throw new NotSupportedException();

        public ValueTask<WorkOsApiKeySummary?> GetApiKeyAsync(string organizationId, string apiKeyId, CancellationToken ct = default)
            => throw new NotSupportedException();

        public ValueTask RevokeApiKeyAsync(string organizationId, string apiKeyId, CancellationToken ct = default)
            => throw new NotSupportedException();

        public ValueTask<bool> IsOrganizationAdminAsync(string organizationId, string subject, CancellationToken ct = default)
            => ValueTask.FromResult(false);
    }

    private sealed class FakeTenantMapper : IWorkOsTenantMapper
    {
        private readonly string? _tenant;

        public FakeTenantMapper(string? tenant)
        {
            _tenant = tenant;
        }

        public ValueTask<string?> GetTenantIdAsync(string organizationId, CancellationToken ct = default)
            => ValueTask.FromResult(_tenant);

        public ValueTask<string?> GetOrganizationIdAsync(string tenantId, CancellationToken ct = default)
            => ValueTask.FromResult<string?>(null);

        public ValueTask SetMappingAsync(string organizationId, string tenantId, CancellationToken ct = default)
            => ValueTask.CompletedTask;
    }
}

