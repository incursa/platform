namespace Incursa.Integrations.WorkOS.AppAuth.Abstractions;

public sealed record WorkOsClaimSet(
    string? Subject,
    IReadOnlyList<string> OrganizationIds,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions);
