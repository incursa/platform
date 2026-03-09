namespace Incursa.Integrations.WorkOS.Abstractions.Claims;

public interface IWorkOsMembershipClient
{
    ValueTask<IReadOnlyCollection<WorkOsOrganizationMembershipInfo>> ListOrganizationMembershipsAsync(
        string userId,
        CancellationToken ct = default);

    ValueTask<IReadOnlyCollection<string>> ListRolePermissionsAsync(
        string organizationId,
        IEnumerable<string> roleSlugs,
        CancellationToken ct = default);
}
