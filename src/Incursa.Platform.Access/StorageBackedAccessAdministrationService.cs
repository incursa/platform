namespace Incursa.Platform.Access;

using System.Globalization;
using Incursa.Platform.Access.Internal;
using Incursa.Platform.Storage;

internal sealed class StorageBackedAccessAdministrationService : IAccessAdministrationService
{
    private readonly AccessStorageContext storage;
    private readonly IAccessRegistry registry;
    private readonly IAccessQueryService queryService;
    private readonly IEffectiveAccessEvaluator evaluator;
    private readonly IAccessAuditJournal auditJournal;
    private readonly TimeProvider timeProvider;

    public StorageBackedAccessAdministrationService(
        AccessStorageContext storage,
        IAccessRegistry registry,
        IAccessQueryService queryService,
        IEffectiveAccessEvaluator evaluator,
        IAccessAuditJournal auditJournal,
        TimeProvider timeProvider)
    {
        this.storage = storage ?? throw new ArgumentNullException(nameof(storage));
        this.registry = registry ?? throw new ArgumentNullException(nameof(registry));
        this.queryService = queryService ?? throw new ArgumentNullException(nameof(queryService));
        this.evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
        this.auditJournal = auditJournal ?? throw new ArgumentNullException(nameof(auditJournal));
        this.timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<AccessUser> UpsertUserAsync(AccessUser user, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        await storage.Users.WriteAsync(
            AccessStorageKeys.User(user.Id),
            user,
            StorageWriteMode.Upsert,
            StorageWriteCondition.Unconditional(),
            cancellationToken).ConfigureAwait(false);

        await AppendAuditAsync(
            "access.user.upserted",
            subjectUserId: user.Id,
            summary: "User upserted.",
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return user;
    }

    public async Task<ScopeRoot> UpsertScopeRootAsync(ScopeRoot scopeRoot, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(scopeRoot);

        var previous = await storage.ScopeRoots.GetAsync(
            AccessStorageKeys.ScopeRoot(scopeRoot.Id),
            cancellationToken).ConfigureAwait(false);

        await storage.ScopeRoots.WriteAsync(
            AccessStorageKeys.ScopeRoot(scopeRoot.Id),
            scopeRoot,
            StorageWriteMode.Upsert,
            StorageWriteCondition.Unconditional(),
            cancellationToken).ConfigureAwait(false);

        if (previous is not null)
        {
            await RemoveScopeRootExternalLinkLookupsAsync(previous.Value, cancellationToken).ConfigureAwait(false);
        }

        await UpsertScopeRootExternalLinkLookupsAsync(scopeRoot, cancellationToken).ConfigureAwait(false);

        await AppendAuditAsync(
            "access.scope-root.upserted",
            subjectUserId: scopeRoot.OwnerUserId,
            resource: AccessResourceReference.ForScopeRoot(scopeRoot.Id),
            summary: "Scope root upserted.",
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return scopeRoot;
    }

    public async Task<Tenant> UpsertTenantAsync(Tenant tenant, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tenant);

        var previous = await storage.Tenants.GetAsync(
            AccessStorageKeys.Tenant(tenant.Id),
            cancellationToken).ConfigureAwait(false);

        await storage.Tenants.WriteAsync(
            AccessStorageKeys.Tenant(tenant.Id),
            tenant,
            StorageWriteMode.Upsert,
            StorageWriteCondition.Unconditional(),
            cancellationToken).ConfigureAwait(false);

        if (previous is not null)
        {
            await storage.TenantsByScopeRoot.DeleteAsync(
                AccessStorageKeys.TenantByScopeRoot(previous.Value.ScopeRootId, previous.Value.Id),
                StorageWriteCondition.Unconditional(),
                cancellationToken).ConfigureAwait(false);

            await RemoveTenantExternalLinkLookupsAsync(previous.Value, cancellationToken).ConfigureAwait(false);
        }

        await storage.TenantsByScopeRoot.UpsertAsync(
            AccessStorageKeys.TenantByScopeRoot(tenant.ScopeRootId, tenant.Id),
            new TenantByScopeRootProjection(tenant),
            StorageWriteCondition.Unconditional(),
            cancellationToken).ConfigureAwait(false);

        await UpsertTenantExternalLinkLookupsAsync(tenant, cancellationToken).ConfigureAwait(false);
        await RefreshAccessibleTenantsForScopeRootAsync(tenant.ScopeRootId, cancellationToken).ConfigureAwait(false);

        await AppendAuditAsync(
            "access.tenant.upserted",
            resource: AccessResourceReference.ForTenant(tenant.Id),
            summary: "Tenant upserted.",
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return tenant;
    }

    public async Task<ScopeMembership> UpsertMembershipAsync(
        ScopeMembership membership,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(membership);

        var previous = await storage.Memberships.GetAsync(
            AccessStorageKeys.Membership(membership.Id),
            cancellationToken).ConfigureAwait(false);

        await storage.Memberships.WriteAsync(
            AccessStorageKeys.Membership(membership.Id),
            membership,
            StorageWriteMode.Upsert,
            StorageWriteCondition.Unconditional(),
            cancellationToken).ConfigureAwait(false);

        if (previous is not null)
        {
            await DeleteMembershipLookupsAsync(previous.Value, cancellationToken).ConfigureAwait(false);
            await RefreshAccessibleTenantsForUserAndScopeRootAsync(
                previous.Value.UserId,
                previous.Value.ScopeRootId,
                cancellationToken).ConfigureAwait(false);
        }

        await UpsertMembershipLookupsAsync(membership, cancellationToken).ConfigureAwait(false);
        await RefreshAccessibleTenantsForUserAndScopeRootAsync(
            membership.UserId,
            membership.ScopeRootId,
            cancellationToken).ConfigureAwait(false);

        await AppendAuditAsync(
            "access.membership.upserted",
            subjectUserId: membership.UserId,
            resource: AccessResourceReference.ForScopeRoot(membership.ScopeRootId),
            summary: "Scope membership upserted.",
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return membership;
    }

    public async Task<bool> DeleteMembershipAsync(
        ScopeMembershipId membershipId,
        CancellationToken cancellationToken = default)
    {
        var existing = await storage.Memberships.GetAsync(
            AccessStorageKeys.Membership(membershipId),
            cancellationToken).ConfigureAwait(false);
        if (existing is null)
        {
            return false;
        }

        await storage.Memberships.DeleteAsync(
            AccessStorageKeys.Membership(membershipId),
            StorageWriteCondition.Unconditional(),
            cancellationToken).ConfigureAwait(false);

        await DeleteMembershipLookupsAsync(existing.Value, cancellationToken).ConfigureAwait(false);
        await RefreshAccessibleTenantsForUserAndScopeRootAsync(
            existing.Value.UserId,
            existing.Value.ScopeRootId,
            cancellationToken).ConfigureAwait(false);

        await AppendAuditAsync(
            "access.membership.deleted",
            subjectUserId: existing.Value.UserId,
            resource: AccessResourceReference.ForScopeRoot(existing.Value.ScopeRootId),
            summary: "Scope membership deleted.",
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return true;
    }

    public async Task<AccessAssignment> UpsertAssignmentAsync(
        AccessAssignment assignment,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(assignment);

        if (!registry.TryGetRole(assignment.RoleId, out _))
        {
            throw new InvalidOperationException($"Role '{assignment.RoleId}' is not registered.");
        }

        var previous = await storage.Assignments.GetAsync(
            AccessStorageKeys.Assignment(assignment.Id),
            cancellationToken).ConfigureAwait(false);

        await storage.Assignments.WriteAsync(
            AccessStorageKeys.Assignment(assignment.Id),
            assignment,
            StorageWriteMode.Upsert,
            StorageWriteCondition.Unconditional(),
            cancellationToken).ConfigureAwait(false);

        if (previous is not null)
        {
            await DeleteAssignmentLookupsAsync(previous.Value, cancellationToken).ConfigureAwait(false);
            await RefreshAccessibleTenantsForAssignmentAsync(previous.Value, cancellationToken).ConfigureAwait(false);
        }

        await UpsertAssignmentLookupsAsync(assignment, cancellationToken).ConfigureAwait(false);
        await RefreshAccessibleTenantsForAssignmentAsync(assignment, cancellationToken).ConfigureAwait(false);

        await AppendAuditAsync(
            "access.assignment.upserted",
            subjectUserId: assignment.UserId,
            resource: assignment.Resource,
            summary: $"Role '{assignment.RoleId}' assigned.",
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return assignment;
    }

    public async Task<bool> DeleteAssignmentAsync(
        AccessAssignmentId assignmentId,
        CancellationToken cancellationToken = default)
    {
        var existing = await storage.Assignments.GetAsync(
            AccessStorageKeys.Assignment(assignmentId),
            cancellationToken).ConfigureAwait(false);
        if (existing is null)
        {
            return false;
        }

        await storage.Assignments.DeleteAsync(
            AccessStorageKeys.Assignment(assignmentId),
            StorageWriteCondition.Unconditional(),
            cancellationToken).ConfigureAwait(false);

        await DeleteAssignmentLookupsAsync(existing.Value, cancellationToken).ConfigureAwait(false);
        await RefreshAccessibleTenantsForAssignmentAsync(existing.Value, cancellationToken).ConfigureAwait(false);

        await AppendAuditAsync(
            "access.assignment.deleted",
            subjectUserId: existing.Value.UserId,
            resource: existing.Value.Resource,
            summary: $"Role '{existing.Value.RoleId}' removed.",
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return true;
    }

    public async Task<ExplicitPermissionGrant> UpsertExplicitPermissionGrantAsync(
        ExplicitPermissionGrant grant,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(grant);

        if (!registry.TryGetPermission(grant.PermissionId, out _))
        {
            throw new InvalidOperationException($"Permission '{grant.PermissionId}' is not registered.");
        }

        var previous = await storage.Grants.GetAsync(
            AccessStorageKeys.Grant(grant.Id),
            cancellationToken).ConfigureAwait(false);

        await storage.Grants.WriteAsync(
            AccessStorageKeys.Grant(grant.Id),
            grant,
            StorageWriteMode.Upsert,
            StorageWriteCondition.Unconditional(),
            cancellationToken).ConfigureAwait(false);

        if (previous is not null)
        {
            await DeleteGrantLookupsAsync(previous.Value, cancellationToken).ConfigureAwait(false);
            await RefreshAccessibleTenantsForGrantAsync(previous.Value, cancellationToken).ConfigureAwait(false);
        }

        await UpsertGrantLookupsAsync(grant, cancellationToken).ConfigureAwait(false);
        await RefreshAccessibleTenantsForGrantAsync(grant, cancellationToken).ConfigureAwait(false);

        await AppendAuditAsync(
            "access.permission-grant.upserted",
            subjectUserId: grant.UserId,
            resource: grant.Resource,
            summary: $"Permission '{grant.PermissionId}' granted.",
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return grant;
    }

    public async Task<bool> DeleteExplicitPermissionGrantAsync(
        ExplicitPermissionGrantId grantId,
        CancellationToken cancellationToken = default)
    {
        var existing = await storage.Grants.GetAsync(
            AccessStorageKeys.Grant(grantId),
            cancellationToken).ConfigureAwait(false);
        if (existing is null)
        {
            return false;
        }

        await storage.Grants.DeleteAsync(
            AccessStorageKeys.Grant(grantId),
            StorageWriteCondition.Unconditional(),
            cancellationToken).ConfigureAwait(false);

        await DeleteGrantLookupsAsync(existing.Value, cancellationToken).ConfigureAwait(false);
        await RefreshAccessibleTenantsForGrantAsync(existing.Value, cancellationToken).ConfigureAwait(false);

        await AppendAuditAsync(
            "access.permission-grant.deleted",
            subjectUserId: existing.Value.UserId,
            resource: existing.Value.Resource,
            summary: $"Permission '{existing.Value.PermissionId}' removed.",
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return true;
    }

    public Task EnqueueWorkItemAsync(AccessWorkItem workItem, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workItem);

        return storage.WorkItems.EnqueueAsync(
            new WorkItem<AccessWorkItem>(workItem.Id, workItem, schemaVersion: "v1"),
            cancellationToken: cancellationToken);
    }

    private async Task RefreshAccessibleTenantsForScopeRootAsync(
        ScopeRootId scopeRootId,
        CancellationToken cancellationToken)
    {
        var memberships = await AccessAsyncEnumerable.ToListAsync(
            queryService.GetMembershipsForScopeRootAsync(scopeRootId, cancellationToken),
            cancellationToken).ConfigureAwait(false);

        HashSet<AccessUserId> userIds = memberships.Select(static item => item.UserId).ToHashSet();

        await foreach (var assignment in queryService.GetAssignmentsForResourceAsync(
                           AccessResourceReference.ForScopeRoot(scopeRootId),
                           cancellationToken).ConfigureAwait(false))
        {
            userIds.Add(assignment.UserId);
        }

        foreach (var userId in userIds)
        {
            await RefreshAccessibleTenantsForUserAndScopeRootAsync(userId, scopeRootId, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task RefreshAccessibleTenantsForAssignmentAsync(
        AccessAssignment assignment,
        CancellationToken cancellationToken)
    {
        switch (assignment.Resource.Kind)
        {
            case AccessResourceKind.ScopeRoot:
                await RefreshAccessibleTenantsForUserAndScopeRootAsync(
                    assignment.UserId,
                    assignment.Resource.ScopeRootId!.Value,
                    cancellationToken).ConfigureAwait(false);
                break;
            case AccessResourceKind.Tenant:
                await RefreshAccessibleTenantAsync(assignment.UserId, assignment.Resource.TenantId!.Value, cancellationToken)
                    .ConfigureAwait(false);
                break;
        }
    }

    private async Task RefreshAccessibleTenantsForGrantAsync(
        ExplicitPermissionGrant grant,
        CancellationToken cancellationToken)
    {
        if (grant.Resource.Kind == AccessResourceKind.Tenant)
        {
            await RefreshAccessibleTenantAsync(grant.UserId, grant.Resource.TenantId!.Value, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private async Task RefreshAccessibleTenantsForUserAndScopeRootAsync(
        AccessUserId userId,
        ScopeRootId scopeRootId,
        CancellationToken cancellationToken)
    {
        await foreach (var tenant in queryService.GetTenantsForScopeRootAsync(scopeRootId, cancellationToken).ConfigureAwait(false))
        {
            await RefreshAccessibleTenantProjectionAsync(userId, tenant, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task RefreshAccessibleTenantAsync(
        AccessUserId userId,
        TenantId tenantId,
        CancellationToken cancellationToken)
    {
        var tenant = await queryService.GetTenantAsync(tenantId, cancellationToken).ConfigureAwait(false);
        if (tenant is not null)
        {
            await RefreshAccessibleTenantProjectionAsync(userId, tenant, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task RefreshAccessibleTenantProjectionAsync(
        AccessUserId userId,
        Tenant tenant,
        CancellationToken cancellationToken)
    {
        var effectiveAccess = await evaluator.EvaluateAsync(
            new AccessEvaluationRequest(userId, AccessResourceReference.ForTenant(tenant.Id)),
            cancellationToken).ConfigureAwait(false);

        var key = AccessStorageKeys.AccessibleTenantByUser(userId, tenant.Id);
        if (effectiveAccess.IsAllowed)
        {
            await storage.AccessibleTenantsByUser.UpsertAsync(
                key,
                new AccessibleTenantByUserProjection(tenant),
                StorageWriteCondition.Unconditional(),
                cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await storage.AccessibleTenantsByUser.DeleteAsync(
                key,
                StorageWriteCondition.Unconditional(),
                cancellationToken).ConfigureAwait(false);
        }
    }

    private Task UpsertMembershipLookupsAsync(ScopeMembership membership, CancellationToken cancellationToken) =>
        Task.WhenAll(
            storage.MembershipsByUser.UpsertAsync(
                AccessStorageKeys.MembershipByUser(membership.UserId, membership.Id),
                new MembershipByUserProjection(membership),
                StorageWriteCondition.Unconditional(),
                cancellationToken),
            storage.MembershipsByScopeRoot.UpsertAsync(
                AccessStorageKeys.MembershipByScopeRoot(membership.ScopeRootId, membership.Id),
                new MembershipByScopeRootProjection(membership),
                StorageWriteCondition.Unconditional(),
                cancellationToken));

    private Task DeleteMembershipLookupsAsync(ScopeMembership membership, CancellationToken cancellationToken) =>
        Task.WhenAll(
            storage.MembershipsByUser.DeleteAsync(
                AccessStorageKeys.MembershipByUser(membership.UserId, membership.Id),
                StorageWriteCondition.Unconditional(),
                cancellationToken),
            storage.MembershipsByScopeRoot.DeleteAsync(
                AccessStorageKeys.MembershipByScopeRoot(membership.ScopeRootId, membership.Id),
                StorageWriteCondition.Unconditional(),
                cancellationToken));

    private Task UpsertAssignmentLookupsAsync(AccessAssignment assignment, CancellationToken cancellationToken) =>
        Task.WhenAll(
            storage.AssignmentsByUser.UpsertAsync(
                AccessStorageKeys.AssignmentByUser(assignment.UserId, assignment.Id),
                new AssignmentByUserProjection(assignment),
                StorageWriteCondition.Unconditional(),
                cancellationToken),
            storage.AssignmentsByResource.UpsertAsync(
                AccessStorageKeys.AssignmentByResource(assignment.Resource, assignment.Id),
                new AssignmentByResourceProjection(assignment),
                StorageWriteCondition.Unconditional(),
                cancellationToken));

    private Task DeleteAssignmentLookupsAsync(AccessAssignment assignment, CancellationToken cancellationToken) =>
        Task.WhenAll(
            storage.AssignmentsByUser.DeleteAsync(
                AccessStorageKeys.AssignmentByUser(assignment.UserId, assignment.Id),
                StorageWriteCondition.Unconditional(),
                cancellationToken),
            storage.AssignmentsByResource.DeleteAsync(
                AccessStorageKeys.AssignmentByResource(assignment.Resource, assignment.Id),
                StorageWriteCondition.Unconditional(),
                cancellationToken));

    private Task UpsertGrantLookupsAsync(ExplicitPermissionGrant grant, CancellationToken cancellationToken) =>
        Task.WhenAll(
            storage.GrantsByUser.UpsertAsync(
                AccessStorageKeys.GrantByUser(grant.UserId, grant.Id),
                new GrantByUserProjection(grant),
                StorageWriteCondition.Unconditional(),
                cancellationToken),
            storage.GrantsByResource.UpsertAsync(
                AccessStorageKeys.GrantByResource(grant.Resource, grant.Id),
                new GrantByResourceProjection(grant),
                StorageWriteCondition.Unconditional(),
                cancellationToken));

    private Task DeleteGrantLookupsAsync(ExplicitPermissionGrant grant, CancellationToken cancellationToken) =>
        Task.WhenAll(
            storage.GrantsByUser.DeleteAsync(
                AccessStorageKeys.GrantByUser(grant.UserId, grant.Id),
                StorageWriteCondition.Unconditional(),
                cancellationToken),
            storage.GrantsByResource.DeleteAsync(
                AccessStorageKeys.GrantByResource(grant.Resource, grant.Id),
                StorageWriteCondition.Unconditional(),
                cancellationToken));

    private async Task UpsertScopeRootExternalLinkLookupsAsync(ScopeRoot scopeRoot, CancellationToken cancellationToken)
    {
        foreach (var link in scopeRoot.ExternalLinks)
        {
            await storage.ScopeRootsByExternalLink.UpsertAsync(
                AccessStorageKeys.ScopeRootByExternalLink(link.Provider, link.ExternalId, link.ResourceType),
                new ScopeRootByExternalLinkProjection(scopeRoot),
                StorageWriteCondition.Unconditional(),
                cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task RemoveScopeRootExternalLinkLookupsAsync(ScopeRoot scopeRoot, CancellationToken cancellationToken)
    {
        foreach (var link in scopeRoot.ExternalLinks)
        {
            await storage.ScopeRootsByExternalLink.DeleteAsync(
                AccessStorageKeys.ScopeRootByExternalLink(link.Provider, link.ExternalId, link.ResourceType),
                StorageWriteCondition.Unconditional(),
                cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task UpsertTenantExternalLinkLookupsAsync(Tenant tenant, CancellationToken cancellationToken)
    {
        foreach (var link in tenant.ExternalLinks)
        {
            await storage.TenantsByExternalLink.UpsertAsync(
                AccessStorageKeys.TenantByExternalLink(link.Provider, link.ExternalId, link.ResourceType),
                new TenantByExternalLinkProjection(tenant),
                StorageWriteCondition.Unconditional(),
                cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task RemoveTenantExternalLinkLookupsAsync(Tenant tenant, CancellationToken cancellationToken)
    {
        foreach (var link in tenant.ExternalLinks)
        {
            await storage.TenantsByExternalLink.DeleteAsync(
                AccessStorageKeys.TenantByExternalLink(link.Provider, link.ExternalId, link.ResourceType),
                StorageWriteCondition.Unconditional(),
                cancellationToken).ConfigureAwait(false);
        }
    }

    private Task AppendAuditAsync(
        string action,
        AccessUserId? subjectUserId = null,
        AccessResourceReference? resource = null,
        string? summary = null,
        CancellationToken cancellationToken = default)
    {
        var entry = new AccessAuditEntry(
            new AccessAuditEntryId("audit-" + Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture)),
            timeProvider.GetUtcNow(),
            action,
            subjectUserId: subjectUserId,
            resource: resource,
            summary: summary);

        return auditJournal.AppendAsync(entry, cancellationToken);
    }
}
