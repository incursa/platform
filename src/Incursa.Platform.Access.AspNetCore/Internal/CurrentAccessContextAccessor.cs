namespace Incursa.Platform.Access.AspNetCore;

using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

internal sealed class CurrentAccessContextAccessor : ICurrentAccessContextAccessor
{
    private static readonly object CacheKey = new();

    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly IAccessQueryService queryService;
    private readonly AccessAspNetCoreOptions options;

    public CurrentAccessContextAccessor(
        IHttpContextAccessor httpContextAccessor,
        IAccessQueryService queryService,
        IOptions<AccessAspNetCoreOptions> options)
    {
        this.httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        this.queryService = queryService ?? throw new ArgumentNullException(nameof(queryService));
        ArgumentNullException.ThrowIfNull(options);
        this.options = options.Value;
    }

    public async ValueTask<CurrentAccessContext> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        var httpContext = httpContextAccessor.HttpContext;
        var principal = httpContext?.User ?? new ClaimsPrincipal(new ClaimsIdentity());
        if (httpContext is null)
        {
            return new CurrentAccessContext(principal, null, null, null, null);
        }

        if (httpContext.Items.TryGetValue(CacheKey, out var cached)
            && cached is CurrentAccessContext cachedContext)
        {
            return cachedContext;
        }

        var context = await ResolveAsync(httpContext, principal, cancellationToken).ConfigureAwait(false);
        httpContext.Items[CacheKey] = context;
        return context;
    }

    private async Task<CurrentAccessContext> ResolveAsync(
        HttpContext httpContext,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var accessContext = principal.GetAccessContext(options);

        if (principal.Identity?.IsAuthenticated != true)
        {
            return new CurrentAccessContext(principal, null, null, null, null, accessContext);
        }

        var subject = AccessClaimValueReader.ReadFirst(principal, options.SubjectClaimTypes);
        if (string.IsNullOrWhiteSpace(subject))
        {
            return new CurrentAccessContext(principal, null, null, null, null, accessContext);
        }

        var userId = new AccessUserId(subject);
        var user = await queryService.GetUserAsync(userId, cancellationToken).ConfigureAwait(false);
        var membershipScopeRootIds = await ToScopeRootIdSetAsync(
            queryService.GetMembershipsForUserAsync(userId, cancellationToken),
            cancellationToken).ConfigureAwait(false);

        var scopeRoot = await ResolveScopeRootAsync(
            httpContext,
            principal,
            userId,
            membershipScopeRootIds,
            cancellationToken).ConfigureAwait(false);

        var accessibleTenants = await ToTenantDictionaryAsync(
            queryService.GetAccessibleTenantsAsync(userId, cancellationToken),
            cancellationToken).ConfigureAwait(false);

        var tenant = await ResolveTenantAsync(
            httpContext,
            principal,
            accessibleTenants,
            scopeRoot,
            cancellationToken).ConfigureAwait(false);

        if (tenant is not null && scopeRoot is null)
        {
            scopeRoot = await queryService.GetScopeRootAsync(tenant.ScopeRootId, cancellationToken).ConfigureAwait(false);
        }

        return new CurrentAccessContext(principal, userId, user, scopeRoot, tenant, accessContext);
    }

    private async Task<ScopeRoot?> ResolveScopeRootAsync(
        HttpContext httpContext,
        ClaimsPrincipal principal,
        AccessUserId userId,
        IReadOnlySet<ScopeRootId> membershipScopeRootIds,
        CancellationToken cancellationToken)
    {
        var explicitScopeRootId = ResolveRequestValue(httpContext, options.ScopeRootRouteKey, options.ScopeRootQueryKey);
        if (TryCreateScopeRootId(explicitScopeRootId, out var scopeRootId))
        {
            var scopeRoot = await queryService.GetScopeRootAsync(scopeRootId, cancellationToken).ConfigureAwait(false);
            if (IsAccessibleScopeRoot(scopeRoot, userId, membershipScopeRootIds))
            {
                return scopeRoot;
            }
        }

        var allowedExternalIds = AccessClaimValueReader.ReadSet(principal, options.ScopeRootExternalIdClaimTypes);
        var explicitExternalScopeRootId = ResolveRequestValue(
            httpContext,
            options.ScopeRootExternalRouteKey,
            options.ScopeRootExternalQueryKey);
        if (!string.IsNullOrWhiteSpace(explicitExternalScopeRootId)
            && (allowedExternalIds.Count == 0 || allowedExternalIds.Contains(explicitExternalScopeRootId, StringComparer.OrdinalIgnoreCase)))
        {
            var scopeRoot = await GetScopeRootByExternalIdAsync(explicitExternalScopeRootId, cancellationToken).ConfigureAwait(false);
            if (IsAccessibleScopeRoot(scopeRoot, userId, membershipScopeRootIds))
            {
                return scopeRoot;
            }
        }

        foreach (var candidate in AccessClaimValueReader.ReadSet(principal, options.ScopeRootIdClaimTypes))
        {
            if (!TryCreateScopeRootId(candidate, out scopeRootId))
            {
                continue;
            }

            var scopeRoot = await queryService.GetScopeRootAsync(scopeRootId, cancellationToken).ConfigureAwait(false);
            if (IsAccessibleScopeRoot(scopeRoot, userId, membershipScopeRootIds))
            {
                return scopeRoot;
            }
        }

        foreach (var candidate in allowedExternalIds)
        {
            var scopeRoot = await GetScopeRootByExternalIdAsync(candidate, cancellationToken).ConfigureAwait(false);
            if (IsAccessibleScopeRoot(scopeRoot, userId, membershipScopeRootIds))
            {
                return scopeRoot;
            }
        }

        if (!options.UsePersonalScopeFallback)
        {
            return null;
        }

        var personalScope = await queryService.GetPersonalScopeRootAsync(userId, cancellationToken).ConfigureAwait(false);
        return IsAccessibleScopeRoot(personalScope, userId, membershipScopeRootIds) ? personalScope : null;
    }

    private async Task<Tenant?> ResolveTenantAsync(
        HttpContext httpContext,
        ClaimsPrincipal principal,
        IReadOnlyDictionary<TenantId, Tenant> accessibleTenants,
        ScopeRoot? scopeRoot,
        CancellationToken cancellationToken)
    {
        var explicitTenantId = ResolveRequestValue(httpContext, options.TenantRouteKey, options.TenantQueryKey);
        if (TryGetAccessibleTenant(accessibleTenants, explicitTenantId, scopeRoot, out var tenant))
        {
            return tenant;
        }

        var explicitExternalTenantId = ResolveRequestValue(httpContext, options.TenantExternalRouteKey, options.TenantExternalQueryKey);
        if (!string.IsNullOrWhiteSpace(explicitExternalTenantId))
        {
            tenant = await GetAccessibleTenantByExternalIdAsync(
                explicitExternalTenantId,
                accessibleTenants,
                scopeRoot,
                cancellationToken).ConfigureAwait(false);
            if (tenant is not null)
            {
                return tenant;
            }
        }

        foreach (var candidate in AccessClaimValueReader.ReadSet(principal, options.TenantIdClaimTypes))
        {
            if (TryGetAccessibleTenant(accessibleTenants, candidate, scopeRoot, out tenant))
            {
                return tenant;
            }
        }

        foreach (var candidate in AccessClaimValueReader.ReadSet(principal, options.TenantExternalIdClaimTypes))
        {
            tenant = await GetAccessibleTenantByExternalIdAsync(
                candidate,
                accessibleTenants,
                scopeRoot,
                cancellationToken).ConfigureAwait(false);
            if (tenant is not null)
            {
                return tenant;
            }
        }

        return null;
    }

    private string? ResolveRequestValue(HttpContext httpContext, string routeKey, string queryKey)
    {
        if (options.ResolveFromRoute
            && !string.IsNullOrWhiteSpace(routeKey)
            && httpContext.Request.RouteValues.TryGetValue(routeKey, out var routeValue)
            && !string.IsNullOrWhiteSpace(routeValue?.ToString()))
        {
            return routeValue.ToString()!.Trim();
        }

        if (options.ResolveFromQuery
            && !string.IsNullOrWhiteSpace(queryKey)
            && httpContext.Request.Query.TryGetValue(queryKey, out var queryValues))
        {
            var queryValue = queryValues.ToString();
            if (!string.IsNullOrWhiteSpace(queryValue))
            {
                return queryValue.Trim();
            }
        }

        return null;
    }

    private async Task<ScopeRoot?> GetScopeRootByExternalIdAsync(string externalId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(externalId) || string.IsNullOrWhiteSpace(options.ScopeRootExternalLinkProvider))
        {
            return null;
        }

        return await queryService.GetScopeRootByExternalLinkAsync(
            options.ScopeRootExternalLinkProvider,
            externalId,
            options.ScopeRootExternalLinkResourceType,
            cancellationToken).ConfigureAwait(false);
    }

    private async Task<Tenant?> GetAccessibleTenantByExternalIdAsync(
        string externalId,
        IReadOnlyDictionary<TenantId, Tenant> accessibleTenants,
        ScopeRoot? scopeRoot,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(externalId) || string.IsNullOrWhiteSpace(options.TenantExternalLinkProvider))
        {
            return null;
        }

        var tenant = await queryService.GetTenantByExternalLinkAsync(
            options.TenantExternalLinkProvider,
            externalId,
            options.TenantExternalLinkResourceType,
            cancellationToken).ConfigureAwait(false);

        return tenant is not null && accessibleTenants.ContainsKey(tenant.Id) && IsWithinScope(tenant, scopeRoot)
            ? tenant
            : null;
    }

    private static bool TryCreateScopeRootId(string? value, out ScopeRootId scopeRootId)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            scopeRootId = default;
            return false;
        }

        scopeRootId = new ScopeRootId(value);
        return true;
    }

    private static bool TryGetAccessibleTenant(
        IReadOnlyDictionary<TenantId, Tenant> accessibleTenants,
        string? value,
        ScopeRoot? scopeRoot,
        out Tenant? tenant)
    {
        tenant = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var tenantId = new TenantId(value);
        if (!accessibleTenants.TryGetValue(tenantId, out tenant))
        {
            tenant = null;
            return false;
        }

        if (!IsWithinScope(tenant, scopeRoot))
        {
            tenant = null;
            return false;
        }

        return true;
    }

    private static bool IsWithinScope(Tenant tenant, ScopeRoot? scopeRoot) =>
        scopeRoot is null || tenant.ScopeRootId == scopeRoot.Id;

    private static bool IsAccessibleScopeRoot(
        ScopeRoot? scopeRoot,
        AccessUserId userId,
        IReadOnlySet<ScopeRootId> membershipScopeRootIds)
    {
        if (scopeRoot is null)
        {
            return false;
        }

        return scopeRoot.Kind switch
        {
            ScopeRootKind.Personal => scopeRoot.OwnerUserId == userId,
            _ => membershipScopeRootIds.Contains(scopeRoot.Id),
        };
    }

    private static async Task<HashSet<ScopeRootId>> ToScopeRootIdSetAsync(
        IAsyncEnumerable<ScopeMembership> source,
        CancellationToken cancellationToken)
    {
        HashSet<ScopeRootId> scopeRootIds = [];
        await foreach (var membership in source.ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (membership.IsActive)
            {
                scopeRootIds.Add(membership.ScopeRootId);
            }
        }

        return scopeRootIds;
    }

    private static async Task<Dictionary<TenantId, Tenant>> ToTenantDictionaryAsync(
        IAsyncEnumerable<Tenant> source,
        CancellationToken cancellationToken)
    {
        Dictionary<TenantId, Tenant> tenants = [];
        await foreach (var tenant in source.ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();
            tenants[tenant.Id] = tenant;
        }

        return tenants;
    }
}
