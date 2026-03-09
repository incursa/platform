#pragma warning disable MA0048
namespace Incursa.Platform.Access;

public interface IAccessRegistry
{
    AccessRegistrySnapshot Snapshot { get; }

    bool TryGetPermission(AccessPermissionId permissionId, [NotNullWhen(true)] out AccessPermissionDefinition? permission);

    bool TryGetRole(AccessRoleId roleId, [NotNullWhen(true)] out AccessRoleDefinition? role);

    bool TryGetPermissionByProviderAlias(
        string providerAliasKey,
        string aliasValue,
        [NotNullWhen(true)] out AccessPermissionDefinition? permission);

    bool TryGetRoleByProviderAlias(
        string providerAliasKey,
        string aliasValue,
        [NotNullWhen(true)] out AccessRoleDefinition? role);
}

public interface IAccessAdministrationService
{
    Task<AccessUser> UpsertUserAsync(AccessUser user, CancellationToken cancellationToken = default);

    Task<ScopeRoot> UpsertScopeRootAsync(ScopeRoot scopeRoot, CancellationToken cancellationToken = default);

    Task<Tenant> UpsertTenantAsync(Tenant tenant, CancellationToken cancellationToken = default);

    Task<ScopeMembership> UpsertMembershipAsync(ScopeMembership membership, CancellationToken cancellationToken = default);

    Task<bool> DeleteMembershipAsync(ScopeMembershipId membershipId, CancellationToken cancellationToken = default);

    Task<AccessAssignment> UpsertAssignmentAsync(AccessAssignment assignment, CancellationToken cancellationToken = default);

    Task<bool> DeleteAssignmentAsync(AccessAssignmentId assignmentId, CancellationToken cancellationToken = default);

    Task<ExplicitPermissionGrant> UpsertExplicitPermissionGrantAsync(
        ExplicitPermissionGrant grant,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteExplicitPermissionGrantAsync(
        ExplicitPermissionGrantId grantId,
        CancellationToken cancellationToken = default);

    Task EnqueueWorkItemAsync(AccessWorkItem workItem, CancellationToken cancellationToken = default);
}

public interface IAccessQueryService
{
    Task<AccessUser?> GetUserAsync(AccessUserId userId, CancellationToken cancellationToken = default);

    Task<ScopeRoot?> GetScopeRootAsync(ScopeRootId scopeRootId, CancellationToken cancellationToken = default);

    Task<ScopeRoot?> GetPersonalScopeRootAsync(AccessUserId ownerUserId, CancellationToken cancellationToken = default);

    Task<ScopeRoot?> GetScopeRootByExternalLinkAsync(
        string provider,
        string externalId,
        string? resourceType = null,
        CancellationToken cancellationToken = default);

    Task<Tenant?> GetTenantAsync(TenantId tenantId, CancellationToken cancellationToken = default);

    Task<Tenant?> GetTenantByExternalLinkAsync(
        string provider,
        string externalId,
        string? resourceType = null,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<ScopeMembership> GetMembershipsForUserAsync(
        AccessUserId userId,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<ScopeMembership> GetMembershipsForScopeRootAsync(
        ScopeRootId scopeRootId,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<Tenant> GetTenantsForScopeRootAsync(
        ScopeRootId scopeRootId,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<Tenant> GetAccessibleTenantsAsync(
        AccessUserId userId,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<AccessAssignment> GetAssignmentsForUserAsync(
        AccessUserId userId,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<AccessAssignment> GetAssignmentsForResourceAsync(
        AccessResourceReference resource,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<ExplicitPermissionGrant> GetExplicitPermissionGrantsForUserAsync(
        AccessUserId userId,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<ExplicitPermissionGrant> GetExplicitPermissionGrantsForResourceAsync(
        AccessResourceReference resource,
        CancellationToken cancellationToken = default);
}

public interface IEffectiveAccessEvaluator
{
    Task<EffectiveAccess> EvaluateAsync(
        AccessEvaluationRequest request,
        CancellationToken cancellationToken = default);
}

public interface IAccessAuditJournal
{
    Task AppendAsync(AccessAuditEntry entry, CancellationToken cancellationToken = default);

    IAsyncEnumerable<AccessAuditEntry> QueryByUserAsync(
        AccessUserId userId,
        DateTimeOffset? occurredAfterUtc = null,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<AccessAuditEntry> QueryByResourceAsync(
        AccessResourceReference resource,
        DateTimeOffset? occurredAfterUtc = null,
        CancellationToken cancellationToken = default);
}
#pragma warning restore MA0048
