namespace Incursa.Integrations.WorkOS.Abstractions.Mapping;

public interface IWorkOsTenantMapper
{
    ValueTask<string?> GetTenantIdAsync(string organizationId, CancellationToken ct = default);

    ValueTask<string?> GetOrganizationIdAsync(string tenantId, CancellationToken ct = default);

    ValueTask SetMappingAsync(string organizationId, string tenantId, CancellationToken ct = default);
}

