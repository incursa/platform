namespace Incursa.Integrations.WorkOS.AppAuth.AspNetCore.Auth;

using Incursa.Integrations.WorkOS.AppAuth.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

internal sealed class RequireOrganizationSelectionMiddleware : IMiddleware
{
    private readonly IOrganizationContextAccessor contextAccessor;
    private readonly WorkOsAppAuthOptions options;

    public RequireOrganizationSelectionMiddleware(
        IOrganizationContextAccessor contextAccessor,
        IOptions<WorkOsAppAuthOptions> options)
    {
        ArgumentNullException.ThrowIfNull(contextAccessor);
        ArgumentNullException.ThrowIfNull(options);
        this.contextAccessor = contextAccessor;
        this.options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        if (!this.options.RequireOrganizationSelection)
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        var org = this.contextAccessor.Current;
        if (context.User?.Identity?.IsAuthenticated == true
            && (org is null || string.IsNullOrWhiteSpace(org.SelectedOrganizationId)))
        {
            if (IsApiRequest(context))
            {
                context.Response.StatusCode = StatusCodes.Status409Conflict;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(
                    new
                    {
                        code = "organization_required",
                        message = "Select an organization before accessing this resource.",
                    },
                    context.RequestAborted).ConfigureAwait(false);
                return;
            }

            context.Response.Redirect("/onboarding");
            return;
        }

        await next(context).ConfigureAwait(false);
    }

    private static bool IsApiRequest(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        if (path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var accept = context.Request.Headers.Accept.ToString();
        return accept.Contains("application/json", StringComparison.OrdinalIgnoreCase)
            && !accept.Contains("text/html", StringComparison.OrdinalIgnoreCase);
    }
}
