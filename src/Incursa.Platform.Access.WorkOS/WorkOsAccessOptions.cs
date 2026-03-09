namespace Incursa.Platform.Access.WorkOS;

public sealed class WorkOsAccessOptions
{
    public string ProviderName { get; set; } = WorkOsAccessDefaults.ProviderName;

    public string OrganizationResourceType { get; set; } = WorkOsAccessDefaults.OrganizationResourceType;

    public string RoleAliasKey { get; set; } = WorkOsAccessDefaults.RoleAliasKey;

    public AccessInheritanceMode AssignmentInheritanceMode { get; set; } = AccessInheritanceMode.DescendantTenants;

    public bool CreateMissingScopeRoots { get; set; } = true;

    public bool ReconcileMissingMemberships { get; set; } = true;

    public bool ReconcileMissingAssignments { get; set; } = true;
}
