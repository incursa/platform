namespace Incursa.Integrations.WorkOS.Access;

public sealed record WorkOsOrganizationMembership
{
    public WorkOsOrganizationMembership(
        string organizationId,
        IReadOnlyCollection<string>? roleSlugs = null,
        string? organizationDisplayName = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(organizationId);

        OrganizationId = organizationId.Trim();
        RoleSlugs = roleSlugs?
            .Where(static item => !string.IsNullOrWhiteSpace(item))
            .Select(static item => item.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray()
            ?? Array.Empty<string>();
        OrganizationDisplayName = string.IsNullOrWhiteSpace(organizationDisplayName) ? null : organizationDisplayName.Trim();
    }

    public string OrganizationId { get; }

    public IReadOnlyCollection<string> RoleSlugs { get; }

    public string? OrganizationDisplayName { get; }
}
