namespace Incursa.Platform.Access.AspNetCore;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

public static class AccessHttpContextExtensions
{
    public static Task SignInAccessAsync(
        this HttpContext httpContext,
        AccessAuthenticatedSession session,
        AuthenticationProperties? properties = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        return httpContext.RequestServices
            .GetRequiredService<IAccessAuthenticationTicketService>()
            .SignInAsync(httpContext, session, properties, cancellationToken);
    }

    public static Task<AccessSignOutResult> SignOutAccessAsync(
        this HttpContext httpContext,
        AccessSignOutRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        return httpContext.RequestServices
            .GetRequiredService<IAccessAuthenticationTicketService>()
            .SignOutAsync(httpContext, request, cancellationToken);
    }
}
