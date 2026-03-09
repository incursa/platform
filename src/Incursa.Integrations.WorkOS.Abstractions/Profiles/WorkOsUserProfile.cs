namespace Incursa.Integrations.WorkOS.Abstractions.Profiles;

public sealed record WorkOsUserProfile(
    string Subject,
    IReadOnlyCollection<string> OrganizationIds,
    IReadOnlyDictionary<string, IReadOnlyCollection<string>> RolesByOrganization,
    IReadOnlyCollection<string> Permissions,
    IReadOnlyDictionary<string, string> Metadata,
    DateTimeOffset HydratedUtc);
