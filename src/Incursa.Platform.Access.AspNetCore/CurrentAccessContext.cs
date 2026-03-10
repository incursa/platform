namespace Incursa.Platform.Access.AspNetCore;

using System.Security.Claims;

public sealed record CurrentAccessContext(
    ClaimsPrincipal Principal,
    AccessUserId? UserId,
    AccessUser? User,
    ScopeRoot? ScopeRoot,
    Tenant? Tenant,
    AccessContext? AccessContext = null)
{
    public bool IsAuthenticated => Principal.Identity?.IsAuthenticated == true;
}
