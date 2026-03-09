namespace Incursa.Integrations.WorkOS.AppAuth.AspNetCore.Auth;

using Incursa.Integrations.WorkOS.AppAuth.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

internal sealed class OrganizationContextMiddleware : IMiddleware
{
    private readonly IWorkOsClaimsAccessor claimsAccessor;
    private readonly IOrganizationSelectionStore selectionStore;
    private readonly IWorkOsOrganizationMembershipResolver membershipResolver;
    private readonly IOrganizationContextSetter contextSetter;
    private readonly WorkOsAppAuthOptions options;

    public OrganizationContextMiddleware(
        IWorkOsClaimsAccessor claimsAccessor,
        IOrganizationSelectionStore selectionStore,
        IWorkOsOrganizationMembershipResolver membershipResolver,
        IOrganizationContextAccessor contextAccessor,
        IOptions<WorkOsAppAuthOptions> options)
    {
        ArgumentNullException.ThrowIfNull(claimsAccessor);
        ArgumentNullException.ThrowIfNull(selectionStore);
        ArgumentNullException.ThrowIfNull(membershipResolver);
        ArgumentNullException.ThrowIfNull(contextAccessor);
        ArgumentNullException.ThrowIfNull(options);

        this.claimsAccessor = claimsAccessor;
        this.selectionStore = selectionStore;
        this.membershipResolver = membershipResolver;
        this.contextSetter = contextAccessor as IOrganizationContextSetter
            ?? throw new InvalidOperationException("IOrganizationContextAccessor must also implement IOrganizationContextSetter.");
        this.options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        if (context.User?.Identity?.IsAuthenticated != true)
        {
            this.contextSetter.Set(null);
            await next(context).ConfigureAwait(false);
            return;
        }

        var claims = this.claimsAccessor.Read(context.User);
        var selectedOrg = await ResolveSelectedOrgAsync(context, claims.OrganizationIds).ConfigureAwait(false);

        if (!string.IsNullOrWhiteSpace(selectedOrg))
        {
            var isMember = await this.membershipResolver.IsMemberAsync(context.User, selectedOrg, context, context.RequestAborted).ConfigureAwait(false);
            if (!isMember)
            {
                this.selectionStore.Clear(context);
                selectedOrg = claims.OrganizationIds.FirstOrDefault();
            }
            else
            {
                this.selectionStore.Set(context, selectedOrg);
            }
        }

        this.contextSetter.Set(new OrganizationContext(
            AllowedOrganizationIds: claims.OrganizationIds,
            SelectedOrganizationId: selectedOrg,
            Roles: claims.Roles,
            Permissions: claims.Permissions));

        await next(context).ConfigureAwait(false);
    }

    private async Task<string?> ResolveSelectedOrgAsync(HttpContext context, IReadOnlyList<string> allowedOrganizations)
    {
        if (allowedOrganizations.Count == 0)
        {
            return null;
        }

        string? routeValue = null;
        if (this.options.ResolveFromRoute)
        {
            routeValue = context.Request.RouteValues.TryGetValue(this.options.RouteOrganizationKey, out var rv)
                ? rv?.ToString()
                : null;
        }

        if (IsAllowed(routeValue, allowedOrganizations))
        {
            return routeValue!.Trim();
        }

        string? queryValue = null;
        if (this.options.ResolveFromQuery)
        {
            queryValue = context.Request.Query.TryGetValue(this.options.QueryOrganizationKey, out var qv)
                ? qv.ToString()
                : null;
        }

        if (IsAllowed(queryValue, allowedOrganizations))
        {
            return queryValue!.Trim();
        }

        var selected = this.selectionStore.Get(context);
        if (IsAllowed(selected, allowedOrganizations))
        {
            return selected!.Trim();
        }

        await Task.CompletedTask.ConfigureAwait(false);
        return allowedOrganizations[0];
    }

    private static bool IsAllowed(string? candidate, IReadOnlyList<string> allowedOrganizations)
    {
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return false;
        }

        return allowedOrganizations.Contains(candidate.Trim(), StringComparer.OrdinalIgnoreCase);
    }
}
