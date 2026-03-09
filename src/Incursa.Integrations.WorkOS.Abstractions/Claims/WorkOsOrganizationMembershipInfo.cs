namespace Incursa.Integrations.WorkOS.Abstractions.Claims;

public sealed record WorkOsOrganizationMembershipInfo(string OrganizationId, IReadOnlyCollection<string> RoleSlugs);
