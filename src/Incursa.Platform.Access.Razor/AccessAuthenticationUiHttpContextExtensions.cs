namespace Incursa.Platform.Access.Razor;

using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

public static class AccessAuthenticationUiHttpContextExtensions
{
    public static async Task<ClaimsPrincipal?> GetAccessAuthenticationUiPrincipalAsync(this HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var options = httpContext.RequestServices
            .GetRequiredService<IOptions<AccessAuthenticationUiOptions>>()
            .Value;

        if (string.IsNullOrWhiteSpace(options.AuthenticationScheme))
        {
            return null;
        }

        try
        {
            var result = await httpContext
                .AuthenticateAsync(options.AuthenticationScheme)
                .ConfigureAwait(false);
            if (!result.Succeeded || result.Principal?.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            return result.Principal;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }
}
