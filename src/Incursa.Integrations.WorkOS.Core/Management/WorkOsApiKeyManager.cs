namespace Incursa.Integrations.WorkOS.Core.Management;

using Incursa.Integrations.WorkOS.Abstractions.Management;
using Incursa.Integrations.WorkOS.Abstractions.Persistence;
using Incursa.Integrations.WorkOS.Abstractions.Telemetry;
using Incursa.Integrations.WorkOS.Core.Clients;

public sealed class WorkOsApiKeyManager : IWorkOsApiKeyManager
{
    private readonly IWorkOsManagementClient _client;
    private readonly IWorkOsApiKeyMetadataStore _metadataStore;
    private readonly IWorkOsIntegrationTelemetry _telemetry;

    public WorkOsApiKeyManager(
        IWorkOsManagementClient client,
        IWorkOsApiKeyMetadataStore metadataStore,
        IWorkOsIntegrationTelemetry? telemetry = null)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(metadataStore);

        _client = client;
        _metadataStore = metadataStore;
        _telemetry = telemetry ?? NullWorkOsIntegrationTelemetry.Instance;
    }

    public async ValueTask<WorkOsCreatedApiKey> CreateAsync(
        string organizationId,
        string displayName,
        IReadOnlyCollection<string> scopes,
        int? ttlHours,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(organizationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentNullException.ThrowIfNull(scopes);

        var created = await _client.CreateApiKeyAsync(organizationId, displayName, scopes, ttlHours, ct).ConfigureAwait(false);
        var summary = new WorkOsApiKeySummary(
            created.ApiKeyId,
            created.DisplayName,
            created.CreatedUtc,
            created.ExpiresUtc,
            null,
            created.EffectiveScopes,
            created.OrganizationId,
            created.TenantId);

        await _metadataStore.UpsertAsync(summary, ct).ConfigureAwait(false);
        _telemetry.ApiKeyLifecycle("create", organizationId, created.ApiKeyId, true);
        return created;
    }

    public async IAsyncEnumerable<WorkOsApiKeySummary> ListAsync(string organizationId, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(organizationId);

        await foreach (var key in _client.ListApiKeysAsync(organizationId, ct).ConfigureAwait(false))
        {
            await _metadataStore.UpsertAsync(key, ct).ConfigureAwait(false);
            yield return key;
        }
    }

    public async ValueTask<WorkOsApiKeySummary?> GetAsync(string organizationId, string apiKeyId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(organizationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKeyId);

        var remote = await _client.GetApiKeyAsync(organizationId, apiKeyId, ct).ConfigureAwait(false);
        if (remote is not null)
        {
            await _metadataStore.UpsertAsync(remote, ct).ConfigureAwait(false);
            return remote;
        }

        return await _metadataStore.GetAsync(organizationId, apiKeyId, ct).ConfigureAwait(false);
    }

    public async ValueTask RevokeAsync(string organizationId, string apiKeyId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(organizationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKeyId);

        await _client.RevokeApiKeyAsync(organizationId, apiKeyId, ct).ConfigureAwait(false);
        var existing = await _metadataStore.GetAsync(organizationId, apiKeyId, ct).ConfigureAwait(false);
        if (existing is not null)
        {
            await _metadataStore.UpsertAsync(existing with { RevokedUtc = DateTimeOffset.UtcNow }, ct).ConfigureAwait(false);
        }

        _telemetry.ApiKeyLifecycle("revoke", organizationId, apiKeyId, true);
    }
}

