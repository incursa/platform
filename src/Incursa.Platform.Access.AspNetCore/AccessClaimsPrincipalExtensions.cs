namespace Incursa.Platform.Access.AspNetCore;

using System.Security.Claims;

public static class AccessClaimsPrincipalExtensions
{
    public static string? GetAccessSubjectId(this ClaimsPrincipal principal, AccessAspNetCoreOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(principal);
        options ??= new AccessAspNetCoreOptions();
        return AccessClaimValueReader.ReadFirst(principal, options.SubjectClaimTypes);
    }

    public static string? GetAccessSessionId(this ClaimsPrincipal principal, AccessAspNetCoreOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(principal);
        options ??= new AccessAspNetCoreOptions();
        return AccessClaimValueReader.ReadFirst(principal, options.SessionIdClaimTypes);
    }

    public static string? GetAccessOrganizationId(this ClaimsPrincipal principal, AccessAspNetCoreOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(principal);
        options ??= new AccessAspNetCoreOptions();

        var rawValue = AccessClaimValueReader.ReadFirst(principal, options.OrganizationIdClaimTypes);
        if (!string.IsNullOrWhiteSpace(rawValue)
            && !rawValue.TrimStart().StartsWith("[", StringComparison.Ordinal))
        {
            return rawValue;
        }

        return AccessClaimValueReader.ReadSet(principal, options.OrganizationIdClaimTypes).FirstOrDefault();
    }

    public static IReadOnlyCollection<string> GetAccessRoles(this ClaimsPrincipal principal, AccessAspNetCoreOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(principal);
        options ??= new AccessAspNetCoreOptions();
        return AccessClaimValueReader.ReadSet(principal, options.RoleClaimTypes);
    }

    public static IReadOnlyCollection<string> GetAccessPermissions(this ClaimsPrincipal principal, AccessAspNetCoreOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(principal);
        options ??= new AccessAspNetCoreOptions();
        return AccessClaimValueReader.ReadSet(principal, options.PermissionClaimTypes);
    }

    public static IReadOnlyCollection<string> GetAccessFeatureFlags(this ClaimsPrincipal principal, AccessAspNetCoreOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(principal);
        options ??= new AccessAspNetCoreOptions();
        return AccessClaimValueReader.ReadSet(principal, options.FeatureFlagClaimTypes);
    }

    public static IReadOnlyCollection<string> GetAccessEntitlements(this ClaimsPrincipal principal, AccessAspNetCoreOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(principal);
        options ??= new AccessAspNetCoreOptions();
        return AccessClaimValueReader.ReadSet(principal, options.EntitlementClaimTypes);
    }

    public static AccessContext? GetAccessContext(this ClaimsPrincipal principal, AccessAspNetCoreOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(principal);

        var subject = principal.GetAccessSubjectId(options);
        return string.IsNullOrWhiteSpace(subject)
            ? null
            : new AccessContext(
                subject,
                principal.GetAccessSessionId(options),
                principal.GetAccessOrganizationId(options),
                principal.GetAccessRoles(options),
                principal.GetAccessPermissions(options),
                principal.GetAccessFeatureFlags(options),
                principal.GetAccessEntitlements(options));
    }

    public static bool HasAccessRole(this ClaimsPrincipal principal, string role, AccessAspNetCoreOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(principal);
        ArgumentException.ThrowIfNullOrWhiteSpace(role);

        return principal.GetAccessRoles(options).Contains(role.Trim(), StringComparer.OrdinalIgnoreCase);
    }

    public static bool HasAccessPermission(this ClaimsPrincipal principal, string permission, AccessAspNetCoreOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(principal);
        ArgumentException.ThrowIfNullOrWhiteSpace(permission);

        return principal.GetAccessPermissions(options).Contains(permission.Trim(), StringComparer.OrdinalIgnoreCase);
    }
}
