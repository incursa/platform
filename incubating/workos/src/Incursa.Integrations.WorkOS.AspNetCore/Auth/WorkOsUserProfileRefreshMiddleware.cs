namespace Incursa.Integrations.WorkOS.AspNetCore.Auth;

using System.Security.Claims;
using Incursa.Integrations.WorkOS.Abstractions.Claims;
using Incursa.Integrations.WorkOS.Abstractions.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

public sealed class WorkOsUserProfileRefreshMiddleware : IMiddleware
{
    private static readonly object RefreshedKey = new();

    private readonly IWorkOsClaimsEnricher claimsEnricher;
    private readonly WorkOsUserProfileHydrationOptions hydrationOptions;
    private readonly ILogger<WorkOsUserProfileRefreshMiddleware> logger;

    public WorkOsUserProfileRefreshMiddleware(
        IWorkOsClaimsEnricher claimsEnricher,
        WorkOsUserProfileHydrationOptions hydrationOptions,
        ILogger<WorkOsUserProfileRefreshMiddleware> logger)
    {
        ArgumentNullException.ThrowIfNull(claimsEnricher);
        ArgumentNullException.ThrowIfNull(hydrationOptions);
        ArgumentNullException.ThrowIfNull(logger);

        this.claimsEnricher = claimsEnricher;
        this.hydrationOptions = hydrationOptions;
        this.logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        if (hydrationOptions.Enabled
            && hydrationOptions.RevalidateOnRequestIfStale
            && !context.Items.ContainsKey(RefreshedKey)
            && context.User?.Identity is ClaimsIdentity identity
            && identity.IsAuthenticated)
        {
            try
            {
                await claimsEnricher.EnrichAsync(context.User, identity, context.RequestAborted).ConfigureAwait(false);
                context.Items[RefreshedKey] = true;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to revalidate WorkOS user profile claims during request.");
            }
        }

        await next(context).ConfigureAwait(false);
    }
}
