namespace Incursa.Platform.Access;

using Incursa.Platform.Access.Internal;

public sealed class DefaultEffectiveAccessEvaluator : IEffectiveAccessEvaluator
{
    private readonly IAccessQueryService queryService;
    private readonly IAccessRegistry registry;

    public DefaultEffectiveAccessEvaluator(IAccessQueryService queryService, IAccessRegistry registry)
    {
        this.queryService = queryService ?? throw new ArgumentNullException(nameof(queryService));
        this.registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    public async Task<EffectiveAccess> EvaluateAsync(
        AccessEvaluationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var scopeRoot = await ResolveScopeRootAsync(request.Resource, cancellationToken).ConfigureAwait(false);
        if (scopeRoot is null)
        {
            return new EffectiveAccess(request.UserId, request.Resource, false, false, null);
        }

        var memberships = await AccessAsyncEnumerable.ToListAsync(
            queryService.GetMembershipsForUserAsync(request.UserId, cancellationToken),
            cancellationToken).ConfigureAwait(false);

        var membershipSatisfied = memberships.Any(
                membership => membership.IsActive && membership.ScopeRootId == scopeRoot.Id)
            || (scopeRoot.Kind == ScopeRootKind.Personal && scopeRoot.OwnerUserId == request.UserId);

        HashSet<AccessPermissionId> permissions = [];
        List<EffectiveAccessSource> sources = [];

        if (scopeRoot.Kind == ScopeRootKind.Personal && scopeRoot.OwnerUserId == request.UserId)
        {
            sources.Add(new EffectiveAccessSource(
                EffectiveAccessSourceKind.PersonalScopeOwner,
                scopeRoot.Id.Value,
                "Personal scope owner"));
        }
        else if (membershipSatisfied)
        {
            sources.Add(new EffectiveAccessSource(
                EffectiveAccessSourceKind.ScopeMembership,
                scopeRoot.Id.Value,
                "Scope membership"));
        }

        await AddAssignmentPermissionsAsync(
            request,
            scopeRoot.Id,
            permissions,
            sources,
            cancellationToken).ConfigureAwait(false);

        await AddExplicitGrantPermissionsAsync(
            request,
            permissions,
            sources,
            cancellationToken).ConfigureAwait(false);

        var orderedPermissions = permissions
            .OrderBy(static item => item.Value, StringComparer.Ordinal)
            .ToArray();

        var isAllowed = request.RequiredPermissions.Count == 0
            ? orderedPermissions.Length > 0 || sources.Any(static source => source.Kind == EffectiveAccessSourceKind.PersonalScopeOwner)
            : request.RequiredPermissions.All(orderedPermissions.Contains);

        return new EffectiveAccess(
            request.UserId,
            request.Resource,
            isAllowed,
            membershipSatisfied,
            scopeRoot.Id,
            orderedPermissions,
            sources);
    }

    private async Task AddAssignmentPermissionsAsync(
        AccessEvaluationRequest request,
        ScopeRootId scopeRootId,
        HashSet<AccessPermissionId> permissions,
        List<EffectiveAccessSource> sources,
        CancellationToken cancellationToken)
    {
        var directAssignments = await AccessAsyncEnumerable.ToListAsync(
            queryService.GetAssignmentsForResourceAsync(request.Resource, cancellationToken),
            cancellationToken).ConfigureAwait(false);

        foreach (var assignment in directAssignments)
        {
            if (assignment.UserId != request.UserId)
            {
                continue;
            }

            AddRolePermissions(assignment, permissions, sources);
        }

        if (request.Resource.Kind != AccessResourceKind.Tenant)
        {
            return;
        }

        var inheritedAssignments = await AccessAsyncEnumerable.ToListAsync(
            queryService.GetAssignmentsForResourceAsync(
                AccessResourceReference.ForScopeRoot(scopeRootId),
                cancellationToken),
            cancellationToken).ConfigureAwait(false);

        foreach (var assignment in inheritedAssignments)
        {
            if (assignment.UserId != request.UserId || assignment.InheritanceMode != AccessInheritanceMode.DescendantTenants)
            {
                continue;
            }

            AddRolePermissions(assignment, permissions, sources);
        }
    }

    private async Task AddExplicitGrantPermissionsAsync(
        AccessEvaluationRequest request,
        HashSet<AccessPermissionId> permissions,
        List<EffectiveAccessSource> sources,
        CancellationToken cancellationToken)
    {
        await foreach (var grant in queryService.GetExplicitPermissionGrantsForResourceAsync(
                           request.Resource,
                           cancellationToken).ConfigureAwait(false))
        {
            if (grant.UserId != request.UserId)
            {
                continue;
            }

            if (!registry.TryGetPermission(grant.PermissionId, out _))
            {
                continue;
            }

            permissions.Add(grant.PermissionId);
            sources.Add(new EffectiveAccessSource(
                EffectiveAccessSourceKind.ExplicitPermissionGrant,
                grant.Id.Value,
                grant.PermissionId.Value));
        }
    }

    private void AddRolePermissions(
        AccessAssignment assignment,
        HashSet<AccessPermissionId> permissions,
        List<EffectiveAccessSource> sources)
    {
        if (!registry.TryGetRole(assignment.RoleId, out var role))
        {
            return;
        }

        foreach (var permissionId in role.Permissions)
        {
            permissions.Add(permissionId);
        }

        sources.Add(new EffectiveAccessSource(
            EffectiveAccessSourceKind.RoleAssignment,
            assignment.Id.Value,
            assignment.RoleId.Value));
    }

    private async Task<ScopeRoot?> ResolveScopeRootAsync(
        AccessResourceReference resource,
        CancellationToken cancellationToken)
    {
        return resource.Kind switch
        {
            AccessResourceKind.ScopeRoot => await queryService.GetScopeRootAsync(
                resource.ScopeRootId!.Value,
                cancellationToken).ConfigureAwait(false),
            AccessResourceKind.Tenant => await ResolveScopeRootForTenantAsync(
                resource.TenantId!.Value,
                cancellationToken).ConfigureAwait(false),
            _ => null,
        };
    }

    private async Task<ScopeRoot?> ResolveScopeRootForTenantAsync(TenantId tenantId, CancellationToken cancellationToken)
    {
        var tenant = await queryService.GetTenantAsync(tenantId, cancellationToken).ConfigureAwait(false);
        if (tenant is null)
        {
            return null;
        }

        return await queryService.GetScopeRootAsync(tenant.ScopeRootId, cancellationToken).ConfigureAwait(false);
    }
}
