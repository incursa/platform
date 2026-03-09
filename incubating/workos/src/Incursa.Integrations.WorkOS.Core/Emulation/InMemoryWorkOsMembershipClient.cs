namespace Incursa.Integrations.WorkOS.Core.Emulation;

using Incursa.Integrations.WorkOS.Abstractions.Claims;

public sealed class InMemoryWorkOsMembershipClient : IWorkOsMembershipClient
{
    private readonly InMemoryWorkOsState state;

    public InMemoryWorkOsMembershipClient(InMemoryWorkOsState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        this.state = state;
    }

    public ValueTask<IReadOnlyCollection<WorkOsOrganizationMembershipInfo>> ListOrganizationMembershipsAsync(string userId, CancellationToken ct = default)
        => ValueTask.FromResult(state.ListMemberships(userId));

    public ValueTask<IReadOnlyCollection<string>> ListRolePermissionsAsync(string organizationId, IEnumerable<string> roleSlugs, CancellationToken ct = default)
        => ValueTask.FromResult(state.ListRolePermissions(organizationId, roleSlugs));
}
