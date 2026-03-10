namespace Incursa.Platform.Access.AspNetCore;

using System.Security.Claims;

public interface IAccessClaimsPrincipalFactory
{
    ClaimsPrincipal CreatePrincipal(AccessAuthenticatedSession session);
}
