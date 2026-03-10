namespace Incursa.Platform.Access.AspNetCore;

using System.Security.Claims;

public sealed class AccessAspNetCoreOptions
{
    public IReadOnlyList<string> SubjectClaimTypes { get; set; } =
    [
        "sub",
        ClaimTypes.NameIdentifier,
        "workos:user_id",
    ];

    public IReadOnlyList<string> SessionIdClaimTypes { get; set; } =
    [
        AccessClaimTypes.SessionId,
        "sid",
        "session_id",
        "workos_session_id",
    ];

    public IReadOnlyList<string> OrganizationIdClaimTypes { get; set; } =
    [
        AccessClaimTypes.OrganizationId,
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
        AccessClaimTypes.Role,
        ClaimTypes.Role,
        "role",
        "roles",
        "workos:role",
        "workos:roles",
    ];

    public IReadOnlyList<string> PermissionClaimTypes { get; set; } =
    [
        AccessClaimTypes.Permission,
        "permission",
        "permissions",
        "workos:permission",
        "workos:permissions",
    ];

    public IReadOnlyList<string> FeatureFlagClaimTypes { get; set; } =
    [
        AccessClaimTypes.FeatureFlag,
        "feature_flag",
        "feature_flags",
        "featureFlags",
        "workos:feature_flag",
        "workos:feature_flags",
    ];

    public IReadOnlyList<string> EntitlementClaimTypes { get; set; } =
    [
        AccessClaimTypes.Entitlement,
        "entitlement",
        "entitlements",
        "workos:entitlement",
        "workos:entitlements",
    ];

    public IReadOnlyList<string> ScopeRootIdClaimTypes { get; set; } =
    [
        "scope_root_id",
        "scopeRootId",
    ];

    public IReadOnlyList<string> ScopeRootExternalIdClaimTypes { get; set; } =
    [
        AccessClaimTypes.OrganizationId,
        "org_id",
        "organization_id",
        "workos:org_id",
        "workos:organization_id",
        "organization",
        "org_ids",
        "workos:organization_ids",
    ];

    public IReadOnlyList<string> TenantIdClaimTypes { get; set; } =
    [
        "tenant_id",
        "tenantId",
    ];

    public IReadOnlyList<string> TenantExternalIdClaimTypes { get; set; } = [];

    public string ScopeRootRouteKey { get; set; } = "scopeRootId";

    public string ScopeRootQueryKey { get; set; } = "scopeRootId";

    public string ScopeRootExternalRouteKey { get; set; } = "organizationId";

    public string ScopeRootExternalQueryKey { get; set; } = "organizationId";

    public string TenantRouteKey { get; set; } = "tenantId";

    public string TenantQueryKey { get; set; } = "tenantId";

    public string TenantExternalRouteKey { get; set; } = "externalTenantId";

    public string TenantExternalQueryKey { get; set; } = "externalTenantId";

    public bool ResolveFromRoute { get; set; } = true;

    public bool ResolveFromQuery { get; set; } = true;

    public bool UsePersonalScopeFallback { get; set; } = true;

    public string? ScopeRootExternalLinkProvider { get; set; } = "workos";

    public string? ScopeRootExternalLinkResourceType { get; set; } = "organization";

    public string? TenantExternalLinkProvider { get; set; }

    public string? TenantExternalLinkResourceType { get; set; }
}
