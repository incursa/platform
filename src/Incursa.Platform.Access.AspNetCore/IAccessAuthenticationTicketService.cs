namespace Incursa.Platform.Access.AspNetCore;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

public interface IAccessAuthenticationTicketService
{
    Task SignInAsync(
        HttpContext httpContext,
        AccessAuthenticatedSession session,
        AuthenticationProperties? properties = null,
        CancellationToken cancellationToken = default);

    Task<AccessSignOutResult> SignOutAsync(
        HttpContext httpContext,
        AccessSignOutRequest? request = null,
        CancellationToken cancellationToken = default);
}
