namespace Incursa.Integrations.WorkOS.AppAuth.Abstractions;

public sealed record OrganizationContext(
    IReadOnlyList<string> AllowedOrganizationIds,
    string? SelectedOrganizationId,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions)
{
    public bool HasPermission(string permission)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(permission);
        return Permissions.Contains(permission, StringComparer.OrdinalIgnoreCase);
    }
}
