namespace Incursa.Integrations.WorkOS.Core.Claims;

using Incursa.Integrations.WorkOS.Abstractions.Claims;
using Incursa.Integrations.WorkOS.Abstractions.Configuration;

public sealed class WorkOsMembershipClient : IWorkOsMembershipClient
{
    private readonly HttpClient _httpClient;
    private readonly WorkOsManagementOptions _options;

    public WorkOsMembershipClient(HttpClient httpClient, WorkOsManagementOptions options)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(options);

        _httpClient = httpClient;
        _options = options;
    }

    public ValueTask<IReadOnlyCollection<WorkOsOrganizationMembershipInfo>> ListOrganizationMembershipsAsync(string userId, CancellationToken ct = default)
    {
        return WorkOsClaimHydration.ListOrganizationMembershipsAsync(_httpClient, _options.ApiKey, userId, ct);
    }

    public ValueTask<IReadOnlyCollection<string>> ListRolePermissionsAsync(string organizationId, IEnumerable<string> roleSlugs, CancellationToken ct = default)
    {
        return WorkOsClaimHydration.ListRolePermissionsAsync(_httpClient, _options.ApiKey, organizationId, roleSlugs, ct);
    }
}
