namespace Incursa.Integrations.WorkOS.Core.Mapping;

using Incursa.Integrations.WorkOS.Abstractions.Mapping;
using Incursa.Integrations.WorkOS.Abstractions.Persistence;

public sealed class WorkOsTenantMapper : IWorkOsTenantMapper
{
    private readonly IWorkOsOrgTenantMappingStore _store;

    public WorkOsTenantMapper(IWorkOsOrgTenantMappingStore store)
    {
        ArgumentNullException.ThrowIfNull(store);
        _store = store;
    }

    public async ValueTask<string?> GetTenantIdAsync(string organizationId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(organizationId))
        {
            return null;
        }

        var mapping = await _store.GetByOrganizationIdAsync(organizationId.Trim(), ct).ConfigureAwait(false);
        return mapping?.TenantId;
    }

    public async ValueTask<string?> GetOrganizationIdAsync(string tenantId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            return null;
        }

        var mapping = await _store.GetByTenantIdAsync(tenantId.Trim(), ct).ConfigureAwait(false);
        return mapping?.OrganizationId;
    }

    public async ValueTask SetMappingAsync(string organizationId, string tenantId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(organizationId))
        {
            throw new ArgumentException("Organization id is required.", nameof(organizationId));
        }

        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new ArgumentException("Tenant id is required.", nameof(tenantId));
        }

        var now = DateTimeOffset.UtcNow;
        var existing = await _store.GetByOrganizationIdAsync(organizationId.Trim(), ct).ConfigureAwait(false);
        var createdUtc = existing?.CreatedUtc ?? now;
        await _store.UpsertAsync(new WorkOsOrgTenantMapping(organizationId.Trim(), tenantId.Trim(), createdUtc, now), ct).ConfigureAwait(false);
    }
}

