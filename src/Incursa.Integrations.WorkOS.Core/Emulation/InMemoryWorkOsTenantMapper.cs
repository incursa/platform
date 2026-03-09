namespace Incursa.Integrations.WorkOS.Core.Emulation;

using Incursa.Integrations.WorkOS.Abstractions.Mapping;

public sealed class InMemoryWorkOsTenantMapper : IWorkOsTenantMapper
{
    private readonly InMemoryWorkOsState state;

    public InMemoryWorkOsTenantMapper(InMemoryWorkOsState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        this.state = state;
    }

    public ValueTask<string?> GetTenantIdAsync(string organizationId, CancellationToken ct = default)
        => ValueTask.FromResult(state.GetTenantId(organizationId));

    public ValueTask<string?> GetOrganizationIdAsync(string tenantId, CancellationToken ct = default)
        => ValueTask.FromResult(state.GetOrganizationId(tenantId));

    public ValueTask SetMappingAsync(string organizationId, string tenantId, CancellationToken ct = default)
    {
        state.SeedTenantMapping(organizationId, tenantId);
        return ValueTask.CompletedTask;
    }
}
