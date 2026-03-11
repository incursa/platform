namespace Incursa.Platform.Access.Razor;

using Incursa.Platform.Access.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

public static class AccessAuthenticationUiEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapAccessAuthenticationUiEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var uiOptions = endpoints.ServiceProvider
            .GetRequiredService<IOptions<AccessAuthenticationUiOptions>>()
            .Value;

        endpoints.MapGet(uiOptions.Routes.LoginPath, async (HttpContext httpContext, IOptions<AccessAuthenticationUiOptions> resolvedOptions) =>
        {
            var options = resolvedOptions.Value;
            var returnUrl = AccessAuthenticationRequestHelpers.NormalizeReturnUrl(httpContext.Request.Query["ReturnUrl"]);
            var principal = await httpContext.GetAccessAuthenticationUiPrincipalAsync().ConfigureAwait(false);
            if (principal?.Identity?.IsAuthenticated == true)
            {
                return Results.LocalRedirect(returnUrl ?? options.DefaultReturnUrl);
            }

            return Results.LocalRedirect(AppendReturnUrl(options.Routes.SignInPath, returnUrl));
        }).AllowAnonymous();

        endpoints.MapGet(uiOptions.Routes.LogoutPath, async (
            HttpContext httpContext,
            IOptions<AccessAuthenticationUiOptions> resolvedOptions,
            IAccessAuthenticationTicketService ticketService,
            CancellationToken cancellationToken) =>
        {
            return await SignOutAsync(httpContext, resolvedOptions.Value, ticketService, cancellationToken).ConfigureAwait(false);
        }).AllowAnonymous();

        endpoints.MapPost(uiOptions.Routes.SignOutPath, async (
            HttpContext httpContext,
            IOptions<AccessAuthenticationUiOptions> resolvedOptions,
            IAccessAuthenticationTicketService ticketService,
            CancellationToken cancellationToken) =>
        {
            return await SignOutAsync(httpContext, resolvedOptions.Value, ticketService, cancellationToken).ConfigureAwait(false);
        }).AllowAnonymous();

        return endpoints;
    }

    private static async Task<IResult> SignOutAsync(
        HttpContext httpContext,
        AccessAuthenticationUiOptions options,
        IAccessAuthenticationTicketService ticketService,
        CancellationToken cancellationToken)
    {
        var signOut = await ticketService
            .SignOutAsync(
                httpContext,
                new AccessSignOutRequest(
                    ReturnToUri: AccessAuthenticationRequestHelpers.BuildAppAbsoluteUrl(
                        httpContext.Request,
                        options.PublicBaseUrl,
                        options.Routes.LoggedOutPath)),
                cancellationToken)
            .ConfigureAwait(false);

        if (signOut.LogoutUrl is not null)
        {
            return Results.Redirect(signOut.LogoutUrl.ToString());
        }

        return Results.LocalRedirect(options.Routes.LoggedOutPath);
    }

    internal static string AppendReturnUrl(string path, string? returnUrl)
    {
        returnUrl = AccessAuthenticationRequestHelpers.NormalizeReturnUrl(returnUrl);
        return string.IsNullOrWhiteSpace(returnUrl)
            ? path
            : QueryHelpers.AddQueryString(path, "returnUrl", returnUrl);
    }
}
