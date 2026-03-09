namespace Incursa.Integrations.WorkOS.Abstractions.Persistence;

public interface IWorkOsOrgTenantMappingStore
{
    ValueTask<WorkOsOrgTenantMapping?> GetByOrganizationIdAsync(string organizationId, CancellationToken ct = default);

    ValueTask<WorkOsOrgTenantMapping?> GetByTenantIdAsync(string tenantId, CancellationToken ct = default);

    ValueTask UpsertAsync(WorkOsOrgTenantMapping mapping, CancellationToken ct = default);
}

