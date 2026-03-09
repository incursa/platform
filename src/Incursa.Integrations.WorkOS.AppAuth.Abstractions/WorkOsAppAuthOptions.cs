namespace Incursa.Integrations.WorkOS.AppAuth.Abstractions;

using System.Security.Claims;

public sealed class WorkOsAppAuthOptions
{
    public IReadOnlyList<string> OrganizationClaimTypes { get; set; } =
    [
        "org_id",
        "organization_id",
        "workos:org_id",
        "workos:organization_id",
        "organization",
        "org_ids",
        "workos:organization_ids",
    ];

    public IReadOnlyList<string> RoleClaimTypes { get; set; } =
    [
        "workos:role",
        "role",
        ClaimTypes.Role,
        "roles",
        "workos:roles",
    ];

    public IReadOnlyList<string> PermissionClaimTypes { get; set; } =
    [
        "workos:permission",
        "permissions",
        "permission",
        "workos:permissions",
    ];

    public IReadOnlyList<string> SubjectClaimTypes { get; set; } =
    [
        "sub",
        ClaimTypes.NameIdentifier,
        "workos:user_id",
    ];

    public string QueryOrganizationKey { get; set; } = "organizationId";

    public string RouteOrganizationKey { get; set; } = "organizationId";

    public string CookieOrganizationKey { get; set; } = "Incursa.SelectedOrg";

    public string SessionOrganizationKey { get; set; } = "incursa.selected_org";

    public bool EnableSessionSelection { get; set; }

    public bool ResolveFromRoute { get; set; } = true;

    public bool ResolveFromQuery { get; set; } = true;

    public bool ResolveFromCookie { get; set; } = true;

    public bool ResolveFromSession { get; set; } = true;

    public bool RequireOrganizationSelection { get; set; } = true;

    public string PermissionPolicyPrefix { get; set; } = "perm:";
}
