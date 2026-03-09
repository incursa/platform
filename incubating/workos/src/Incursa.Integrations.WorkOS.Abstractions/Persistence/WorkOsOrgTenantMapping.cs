namespace Incursa.Integrations.WorkOS.Abstractions.Persistence;

public sealed record WorkOsOrgTenantMapping(string OrganizationId, string TenantId, DateTimeOffset CreatedUtc, DateTimeOffset UpdatedUtc);

