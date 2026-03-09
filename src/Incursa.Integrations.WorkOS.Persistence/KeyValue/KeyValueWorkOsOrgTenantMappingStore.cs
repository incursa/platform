namespace Incursa.Integrations.WorkOS.Persistence.KeyValue;

using Incursa.Integrations.WorkOS.Abstractions.Persistence;

public sealed class KeyValueWorkOsOrgTenantMappingStore : IWorkOsOrgTenantMappingStore
{
    private readonly IWorkOsKeyValueStore _store;

    public KeyValueWorkOsOrgTenantMappingStore(IWorkOsKeyValueStore store)
    {
        ArgumentNullException.ThrowIfNull(store);
        _store = store;
    }

    public async ValueTask<WorkOsOrgTenantMapping?> GetByOrganizationIdAsync(string organizationId, CancellationToken ct = default)
    {
        var value = await _store.GetStringAsync(BuildOrgKey(organizationId), ct).ConfigureAwait(false);
        return Deserialize(value);
    }

    public async ValueTask<WorkOsOrgTenantMapping?> GetByTenantIdAsync(string tenantId, CancellationToken ct = default)
    {
        var value = await _store.GetStringAsync(BuildTenantKey(tenantId), ct).ConfigureAwait(false);
        return Deserialize(value);
    }

    public async ValueTask UpsertAsync(WorkOsOrgTenantMapping mapping, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(mapping);
        await _store.SetStringAsync(BuildOrgKey(mapping.OrganizationId), json, null, ct).ConfigureAwait(false);
        await _store.SetStringAsync(BuildTenantKey(mapping.TenantId), json, null, ct).ConfigureAwait(false);
    }

    private static WorkOsOrgTenantMapping? Deserialize(string? raw)
        => string.IsNullOrWhiteSpace(raw) ? null : JsonSerializer.Deserialize<WorkOsOrgTenantMapping>(raw);

    private static string BuildOrgKey(string orgId) => "workos:map:org:" + orgId;

    private static string BuildTenantKey(string tenantId) => "workos:map:tenant:" + tenantId;
}

