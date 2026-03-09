namespace Incursa.Integrations.WorkOS.AspNetCore.Widgets.Services;

using Incursa.Integrations.WorkOS.Abstractions.Widgets;
using Microsoft.AspNetCore.Http;

public sealed class HttpContextWorkOsCurrentSessionIdResolver : IWorkOsCurrentSessionIdResolver
{
    private static readonly string[] ClaimTypes = ["sid", "session_id", "workos_session_id"];
    private readonly IHttpContextAccessor httpContextAccessor;

    public HttpContextWorkOsCurrentSessionIdResolver(IHttpContextAccessor httpContextAccessor)
    {
        ArgumentNullException.ThrowIfNull(httpContextAccessor);
        this.httpContextAccessor = httpContextAccessor;
    }

    public Task<string?> ResolveAsync(CancellationToken cancellationToken)
    {
        var user = httpContextAccessor.HttpContext?.User;
        if (user is null)
        {
            return Task.FromResult<string?>(null);
        }

        foreach (var claimType in ClaimTypes)
        {
            var value = user.FindFirst(claimType)?.Value;
            if (!string.IsNullOrWhiteSpace(value))
            {
                return Task.FromResult<string?>(value);
            }
        }

        return Task.FromResult<string?>(null);
    }
}
