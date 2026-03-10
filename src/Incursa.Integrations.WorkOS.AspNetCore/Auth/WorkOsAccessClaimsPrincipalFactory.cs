namespace Incursa.Integrations.WorkOS.AspNetCore.Auth;

using System.Security.Claims;
using Incursa.Platform.Access;
using Incursa.Platform.Access.AspNetCore;
using Microsoft.Extensions.Options;

internal sealed class WorkOsAccessClaimsPrincipalFactory : IAccessClaimsPrincipalFactory
{
    private readonly AccessSessionCookieOptions options;

    public WorkOsAccessClaimsPrincipalFactory(IOptions<AccessSessionCookieOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        this.options = options.Value;
    }

    public ClaimsPrincipal CreatePrincipal(AccessAuthenticatedSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        var identity = new ClaimsIdentity(options.AuthenticationScheme);
        AddIfPresent(identity, "sub", session.SubjectId);
        AddIfPresent(identity, ClaimTypes.NameIdentifier, session.SubjectId);
        AddIfPresent(identity, ClaimTypes.Email, session.Email);
        AddIfPresent(identity, "email", session.Email);
        AddIfPresent(identity, ClaimTypes.Name, session.DisplayName);
        AddIfPresent(identity, "name", session.DisplayName);
        AddIfPresent(identity, AccessClaimTypes.SessionId, session.SessionId);
        AddIfPresent(identity, "sid", session.SessionId);
        AddIfPresent(identity, "session_id", session.SessionId);
        AddIfPresent(identity, AccessClaimTypes.OrganizationId, session.OrganizationId);
        AddIfPresent(identity, "org_id", session.OrganizationId);
        AddIfPresent(identity, "organization_id", session.OrganizationId);

        foreach (var role in session.Roles)
        {
            identity.AddClaim(new Claim(AccessClaimTypes.Role, role));
            identity.AddClaim(new Claim(ClaimTypes.Role, role));
            identity.AddClaim(new Claim("role", role));
        }

        foreach (var permission in session.Permissions)
        {
            identity.AddClaim(new Claim(AccessClaimTypes.Permission, permission));
            identity.AddClaim(new Claim("permission", permission));
        }

        foreach (var featureFlag in session.FeatureFlags)
        {
            identity.AddClaim(new Claim(AccessClaimTypes.FeatureFlag, featureFlag));
            identity.AddClaim(new Claim("feature_flag", featureFlag));
        }

        foreach (var entitlement in session.Entitlements)
        {
            identity.AddClaim(new Claim(AccessClaimTypes.Entitlement, entitlement));
            identity.AddClaim(new Claim("entitlement", entitlement));
        }

        WorkOsAccessTokenClaims.TryAddClaimsFromAccessToken(session.AccessToken, identity);
        return new ClaimsPrincipal(identity);
    }

    private static void AddIfPresent(ClaimsIdentity identity, string claimType, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            identity.AddClaim(new Claim(claimType, value.Trim()));
        }
    }
}
