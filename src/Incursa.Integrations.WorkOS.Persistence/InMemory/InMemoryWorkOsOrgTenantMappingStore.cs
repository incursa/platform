namespace Incursa.Integrations.WorkOS.Persistence.InMemory;

using Incursa.Integrations.WorkOS.Abstractions.Persistence;

public sealed class InMemoryWorkOsOrgTenantMappingStore : IWorkOsOrgTenantMappingStore
{
    private readonly Dictionary<string, WorkOsOrgTenantMapping> _byOrg = new(StringComparer.Ordinal);
    private readonly Dictionary<string, WorkOsOrgTenantMapping> _byTenant = new(StringComparer.Ordinal);
    private readonly Lock _gate = new();

    public ValueTask<WorkOsOrgTenantMapping?> GetByOrganizationIdAsync(string organizationId, CancellationToken ct = default)
    {
        lock (_gate)
        {
            return ValueTask.FromResult(_byOrg.TryGetValue(organizationId, out var value) ? value : null);
        }
    }

    public ValueTask<WorkOsOrgTenantMapping?> GetByTenantIdAsync(string tenantId, CancellationToken ct = default)
    {
        lock (_gate)
        {
            return ValueTask.FromResult(_byTenant.TryGetValue(tenantId, out var value) ? value : null);
        }
    }

    public ValueTask UpsertAsync(WorkOsOrgTenantMapping mapping, CancellationToken ct = default)
    {
        lock (_gate)
        {
            _byOrg[mapping.OrganizationId] = mapping;
            _byTenant[mapping.TenantId] = mapping;
        }

        return ValueTask.CompletedTask;
    }
}

