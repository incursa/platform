namespace Incursa.Integrations.WorkOS.Tests;

using Incursa.Integrations.WorkOS.Abstractions.Configuration;
using Incursa.Integrations.WorkOS.Abstractions.Mapping;
using Incursa.Integrations.WorkOS.Core.Clients;

[TestClass]
public sealed class WorkOsLiveIntegrationTests
{
    [TestMethod]
    [TestCategory("Integration")]
    public async Task ValidateApiKeyAsync_WithStagingCredentials_ReturnsManagedKey()
    {
        if (!TryGetStagingConfig(out var config))
        {
            Assert.Inconclusive("Staging integration env vars are not configured.");
            return;
        }

        using var httpClient = new HttpClient();
        var sut = new WorkOsManagementHttpClient(
            httpClient,
            new WorkOsManagementOptions
            {
                BaseUrl = config.BaseUrl,
                ApiKey = config.ManagementApiKey,
                RequestTimeout = TimeSpan.FromSeconds(15),
            },
            new PassThroughTenantMapper(config.DefaultTenantId));

        var result = await sut.ValidateApiKeyAsync(config.ValidationApiKey).ConfigureAwait(false);

        Assert.IsNotNull(result, "WorkOS validate endpoint returned null for staging validation key.");
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.ApiKeyId));
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.OrganizationId));
    }

    private static bool TryGetStagingConfig(out StagingConfig config)
    {
        var baseUrl = Environment.GetEnvironmentVariable("WORKOS_STAGING_BASE_URL");
        var managementApiKey = Environment.GetEnvironmentVariable("WORKOS_STAGING_MANAGEMENT_API_KEY");
        var validationApiKey = Environment.GetEnvironmentVariable("WORKOS_STAGING_VALIDATION_API_KEY");
        var defaultTenantId = Environment.GetEnvironmentVariable("WORKOS_STAGING_DEFAULT_TENANT_ID") ?? "staging";

        config = new StagingConfig(
            baseUrl: baseUrl ?? string.Empty,
            managementApiKey: managementApiKey ?? string.Empty,
            validationApiKey: validationApiKey ?? string.Empty,
            defaultTenantId: defaultTenantId);

        return !string.IsNullOrWhiteSpace(config.BaseUrl)
            && !string.IsNullOrWhiteSpace(config.ManagementApiKey)
            && !string.IsNullOrWhiteSpace(config.ValidationApiKey);
    }

    private sealed class PassThroughTenantMapper : IWorkOsTenantMapper
    {
        private readonly string _tenantId;

        public PassThroughTenantMapper(string tenantId)
        {
            _tenantId = tenantId;
        }

        public ValueTask<string?> GetTenantIdAsync(string organizationId, CancellationToken ct = default)
            => ValueTask.FromResult<string?>(_tenantId);

        public ValueTask<string?> GetOrganizationIdAsync(string tenantId, CancellationToken ct = default)
            => ValueTask.FromResult<string?>(null);

        public ValueTask SetMappingAsync(string organizationId, string tenantId, CancellationToken ct = default)
            => ValueTask.CompletedTask;
    }

    private sealed class StagingConfig
    {
        public StagingConfig(string baseUrl, string managementApiKey, string validationApiKey, string defaultTenantId)
        {
            BaseUrl = baseUrl;
            ManagementApiKey = managementApiKey;
            ValidationApiKey = validationApiKey;
            DefaultTenantId = defaultTenantId;
        }

        public string BaseUrl { get; }

        public string ManagementApiKey { get; }

        public string ValidationApiKey { get; }

        public string DefaultTenantId { get; }
    }
}
