namespace Incursa.Integrations.WorkOS.AspNetCore.Auth;

using Incursa.Integrations.WorkOS.Abstractions.Authentication;
using Incursa.Integrations.WorkOS.Abstractions.Authorization;
using Microsoft.AspNetCore.Http;

public sealed class WorkOsRuntimeAuthMiddleware : IMiddleware
{
    private readonly IWorkOsApiKeyAuthenticator _authenticator;
    private readonly IWorkOsScopeAuthorizer _scopeAuthorizer;
    private readonly IWorkOsRequestAuthContextSetter _contextSetter;

    public WorkOsRuntimeAuthMiddleware(
        IWorkOsApiKeyAuthenticator authenticator,
        IWorkOsScopeAuthorizer scopeAuthorizer,
        IWorkOsRequestAuthContextSetter contextSetter)
    {
        ArgumentNullException.ThrowIfNull(authenticator);
        ArgumentNullException.ThrowIfNull(scopeAuthorizer);
        ArgumentNullException.ThrowIfNull(contextSetter);

        _authenticator = authenticator;
        _scopeAuthorizer = scopeAuthorizer;
        _contextSetter = contextSetter;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var presented = ExtractApiKey(context.Request);
        if (string.IsNullOrWhiteSpace(presented))
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        var validation = await _authenticator.ValidateApiKeyAsync(presented, context.RequestAborted).ConfigureAwait(false);
        if (!validation.IsValid || validation.Identity is null)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = validation.ErrorReason }, context.RequestAborted).ConfigureAwait(false);
            return;
        }

        var requiredScope = context.Request.Headers.TryGetValue("X-Required-Scope", out var scopeHeader)
            ? scopeHeader.ToString()
            : string.Empty;
        var requiredTenant = context.Request.Headers.TryGetValue("X-Required-Tenant", out var tenantHeader)
            ? tenantHeader.ToString()
            : validation.Identity.TenantId;

        if (!string.IsNullOrWhiteSpace(requiredScope))
        {
            var authz = await _scopeAuthorizer.AuthorizeAsync(validation.Identity, requiredScope, requiredTenant, context.RequestAborted).ConfigureAwait(false);
            if (!authz.IsValid)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new { error = authz.ErrorReason }, context.RequestAborted).ConfigureAwait(false);
                return;
            }
        }

        _contextSetter.Set(validation.Identity);
        await next(context).ConfigureAwait(false);
    }

    private static string? ExtractApiKey(HttpRequest request)
    {
        if (request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            var authz = authHeader.ToString();
            if (authz.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return authz["Bearer ".Length..].Trim();
            }
        }

        if (request.Headers.TryGetValue("X-NuGet-ApiKey", out var nugetHeader))
        {
            return nugetHeader.ToString().Trim();
        }

        return null;
    }
}

