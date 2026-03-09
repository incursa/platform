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
public sealed class WorkOsApiKeyAuthenticatorBehaviorTests
{
    [TestMethod]
    public async Task ValidateApiKeyAsync_Revoked_ReturnsRevoked()
    {
        var sut = CreateAuthenticator(new FakeManagementClient(_ =>
            new WorkOsManagedApiKey("key_1", "org_1", "test", DateTimeOffset.UtcNow.AddHours(-1), DateTimeOffset.UtcNow.AddHours(1), DateTimeOffset.UtcNow, ["nuget.read"])));

        var result = await sut.ValidateApiKeyAsync("secret").ConfigureAwait(false);

        Assert.IsFalse(result.IsValid);
        Assert.AreEqual(WorkOsValidationErrorCode.Revoked, result.ErrorCode);
    }

    [TestMethod]
    public async Task ValidateApiKeyAsync_Expired_ReturnsExpired()
    {
        var sut = CreateAuthenticator(new FakeManagementClient(_ =>
            new WorkOsManagedApiKey("key_1", "org_1", "test", DateTimeOffset.UtcNow.AddHours(-3), DateTimeOffset.UtcNow.AddMinutes(-1), null, ["nuget.read"])));

        var result = await sut.ValidateApiKeyAsync("secret").ConfigureAwait(false);

        Assert.IsFalse(result.IsValid);
        Assert.AreEqual(WorkOsValidationErrorCode.Expired, result.ErrorCode);
    }

    [TestMethod]
    public async Task ValidateApiKeyAsync_StrictMappingWithUnknownPermission_ReturnsInsufficientScope()
    {
        var options = new WorkOsIntegrationOptions { StrictPermissionMapping = true };
        var mapping = WorkOsPermissionMappingOptions.CreateDefaultArtifacts();
        var mapper = new WorkOsPermissionMapper(mapping);
        var sut = new WorkOsApiKeyAuthenticator(
            new FakeManagementClient(_ => new WorkOsManagedApiKey("key_1", "org_1", "test", DateTimeOffset.UtcNow.AddHours(-1), null, null, ["unknown.permission"])),
            new FakeTenantMapper("tenant-a"),
            mapper,
            new MemoryCache(new MemoryCacheOptions()),
            options);

        var result = await sut.ValidateApiKeyAsync("secret").ConfigureAwait(false);

        Assert.IsFalse(result.IsValid);
        Assert.AreEqual(WorkOsValidationErrorCode.InsufficientScope, result.ErrorCode);
    }

    [TestMethod]
    public async Task ValidateApiKeyAsync_UsesCacheForSubsequentRequests()
    {
        var managementClient = new FakeManagementClient(_ =>
            new WorkOsManagedApiKey("key_1", "org_1", "test", DateTimeOffset.UtcNow.AddHours(-1), DateTimeOffset.UtcNow.AddHours(1), null, ["nuget.read"]));

        var sut = CreateAuthenticator(managementClient);

        var first = await sut.ValidateApiKeyAsync("secret").ConfigureAwait(false);
        var second = await sut.ValidateApiKeyAsync("secret").ConfigureAwait(false);

        Assert.IsTrue(first.IsValid);
        Assert.IsTrue(second.IsValid);
        Assert.AreEqual(1, managementClient.ValidateCalls);
    }

    [TestMethod]
    public async Task ValidateApiKeyAsync_StrictMapping_WithArtifactsPermission_IsValid()
    {
        var options = new WorkOsIntegrationOptions { StrictPermissionMapping = true };
        var mapping = WorkOsPermissionMappingOptions.CreateDefaultArtifacts();
        var mapper = new WorkOsPermissionMapper(mapping);
        var sut = new WorkOsApiKeyAuthenticator(
            new FakeManagementClient(_ => new WorkOsManagedApiKey("key_1", "org_1", "test", DateTimeOffset.UtcNow.AddHours(-1), null, null, ["artifacts:nuget:read"])),
            new FakeTenantMapper("tenant-a"),
            mapper,
            new MemoryCache(new MemoryCacheOptions()),
            options);

        var result = await sut.ValidateApiKeyAsync("secret").ConfigureAwait(false);

        Assert.IsTrue(result.IsValid);
        Assert.IsNotNull(result.Identity);
        CollectionAssert.Contains(result.Identity.Scopes.ToArray(), "nuget.read");
    }

    private static WorkOsApiKeyAuthenticator CreateAuthenticator(IWorkOsManagementClient managementClient)
    {
        var mapping = WorkOsPermissionMappingOptions.CreateDefaultArtifacts();
        var mapper = new WorkOsPermissionMapper(mapping);
        return new WorkOsApiKeyAuthenticator(
            managementClient,
            new FakeTenantMapper("tenant-a"),
            mapper,
            new MemoryCache(new MemoryCacheOptions()),
            new WorkOsIntegrationOptions());
    }

    private sealed class FakeManagementClient : IWorkOsManagementClient
    {
        private readonly Func<string, WorkOsManagedApiKey?> _resolver;

        public FakeManagementClient(Func<string, WorkOsManagedApiKey?> resolver)
        {
            _resolver = resolver;
        }

        public int ValidateCalls { get; private set; }

        public ValueTask<WorkOsManagedApiKey?> ValidateApiKeyAsync(string presentedKey, CancellationToken ct = default)
        {
            ValidateCalls++;
            return ValueTask.FromResult(_resolver(presentedKey));
        }

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
