namespace Incursa.Integrations.WorkOS.Core.Emulation;

using Incursa.Integrations.WorkOS.Abstractions.Claims;
using Incursa.Integrations.WorkOS.Core.Clients;

public interface IInMemoryWorkOsState
{
    void Clear();

    void SeedTenantMapping(string organizationId, string tenantId);

    void SeedApiKey(string secret, WorkOsManagedApiKey apiKey);

    void SeedOrganizationAdmin(string organizationId, string subject);

    void SeedMembership(string userId, WorkOsOrganizationMembershipInfo membership);

    void SeedRolePermissions(string organizationId, string roleSlug, IEnumerable<string> permissions);
}
