namespace Incursa.Platform.Access.AspNetCore;

using System.Security.Claims;
using Microsoft.Extensions.Options;

internal sealed class DefaultAccessClaimsPrincipalFactory : IAccessClaimsPrincipalFactory
{
    private readonly AccessSessionCookieOptions options;

    public DefaultAccessClaimsPrincipalFactory(IOptions<AccessSessionCookieOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        this.options = options.Value;
    }

    public ClaimsPrincipal CreatePrincipal(AccessAuthenticatedSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        List<Claim> claims =
        [
            new("sub", session.SubjectId),
            new(ClaimTypes.NameIdentifier, session.SubjectId),
        ];

        AddClaimIfPresent(claims, ClaimTypes.Email, session.Email);
        AddClaimIfPresent(claims, "email", session.Email);
        AddClaimIfPresent(claims, ClaimTypes.Name, session.DisplayName);
        AddClaimIfPresent(claims, "name", session.DisplayName);
        AddClaimIfPresent(claims, AccessClaimTypes.SessionId, session.SessionId);
        AddClaimIfPresent(claims, "sid", session.SessionId);
        AddClaimIfPresent(claims, "session_id", session.SessionId);
        AddClaimIfPresent(claims, AccessClaimTypes.OrganizationId, session.OrganizationId);
        AddClaimIfPresent(claims, "org_id", session.OrganizationId);
        AddClaimIfPresent(claims, "organization_id", session.OrganizationId);

        foreach (var role in session.Roles)
        {
            claims.Add(new Claim(AccessClaimTypes.Role, role));
            claims.Add(new Claim(ClaimTypes.Role, role));
            claims.Add(new Claim("role", role));
        }

        foreach (var permission in session.Permissions)
        {
            claims.Add(new Claim(AccessClaimTypes.Permission, permission));
            claims.Add(new Claim("permission", permission));
        }

        foreach (var featureFlag in session.FeatureFlags)
        {
            claims.Add(new Claim(AccessClaimTypes.FeatureFlag, featureFlag));
            claims.Add(new Claim("feature_flag", featureFlag));
        }

        foreach (var entitlement in session.Entitlements)
        {
            claims.Add(new Claim(AccessClaimTypes.Entitlement, entitlement));
            claims.Add(new Claim("entitlement", entitlement));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, options.AuthenticationScheme));
    }

    private static void AddClaimIfPresent(ICollection<Claim> claims, string type, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            claims.Add(new Claim(type, value.Trim()));
        }
    }
}
