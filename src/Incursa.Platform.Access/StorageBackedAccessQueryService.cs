namespace Incursa.Platform.Access;

using System.Runtime.CompilerServices;
using Incursa.Platform.Access.Internal;
using Incursa.Platform.Storage;

internal sealed class StorageBackedAccessQueryService : IAccessQueryService
{
    private readonly AccessStorageContext storage;

    public StorageBackedAccessQueryService(AccessStorageContext storage)
    {
        this.storage = storage ?? throw new ArgumentNullException(nameof(storage));
    }

    public async Task<AccessUser?> GetUserAsync(AccessUserId userId, CancellationToken cancellationToken = default)
    {
        var item = await storage.Users.GetAsync(AccessStorageKeys.User(userId), cancellationToken).ConfigureAwait(false);
        return item?.Value;
    }

    public async Task<ScopeRoot?> GetScopeRootAsync(ScopeRootId scopeRootId, CancellationToken cancellationToken = default)
    {
        var item = await storage.ScopeRoots.GetAsync(
            AccessStorageKeys.ScopeRoot(scopeRootId),
            cancellationToken).ConfigureAwait(false);
        return item?.Value;
    }

    public async Task<ScopeRoot?> GetPersonalScopeRootAsync(AccessUserId ownerUserId, CancellationToken cancellationToken = default)
    {
        await foreach (var item in storage.ScopeRoots.QueryPartitionAsync(
                           AccessStorageKeys.ScopeRootPartition(),
                           StoragePartitionQuery.All(),
                           cancellationToken).ConfigureAwait(false))
        {
            if (item.Value.Kind == ScopeRootKind.Personal && item.Value.OwnerUserId == ownerUserId)
            {
                return item.Value;
            }
        }

        return null;
    }

    public async Task<ScopeRoot?> GetScopeRootByExternalLinkAsync(
        string provider,
        string externalId,
        string? resourceType = null,
        CancellationToken cancellationToken = default)
    {
        var item = await storage.ScopeRootsByExternalLink.GetAsync(
            AccessStorageKeys.ScopeRootByExternalLink(provider, externalId, resourceType),
            cancellationToken).ConfigureAwait(false);
        return item?.Value.ScopeRoot;
    }

    public async Task<Tenant?> GetTenantAsync(TenantId tenantId, CancellationToken cancellationToken = default)
    {
        var item = await storage.Tenants.GetAsync(AccessStorageKeys.Tenant(tenantId), cancellationToken).ConfigureAwait(false);
        return item?.Value;
    }

    public async Task<Tenant?> GetTenantByExternalLinkAsync(
        string provider,
        string externalId,
        string? resourceType = null,
        CancellationToken cancellationToken = default)
    {
        var item = await storage.TenantsByExternalLink.GetAsync(
            AccessStorageKeys.TenantByExternalLink(provider, externalId, resourceType),
            cancellationToken).ConfigureAwait(false);
        return item?.Value.Tenant;
    }

    public async IAsyncEnumerable<ScopeMembership> GetMembershipsForUserAsync(
        AccessUserId userId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var item in storage.MembershipsByUser.QueryPartitionAsync(
                           AccessStorageKeys.MembershipByUserPartition(userId),
                           StoragePartitionQuery.All(),
                           cancellationToken).ConfigureAwait(false))
        {
            yield return item.Value.Membership;
        }
    }

    public async IAsyncEnumerable<ScopeMembership> GetMembershipsForScopeRootAsync(
        ScopeRootId scopeRootId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var item in storage.MembershipsByScopeRoot.QueryPartitionAsync(
                           AccessStorageKeys.MembershipByScopeRootPartition(scopeRootId),
                           StoragePartitionQuery.All(),
                           cancellationToken).ConfigureAwait(false))
        {
            yield return item.Value.Membership;
        }
    }

    public async IAsyncEnumerable<Tenant> GetTenantsForScopeRootAsync(
        ScopeRootId scopeRootId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var item in storage.TenantsByScopeRoot.QueryPartitionAsync(
                           AccessStorageKeys.TenantByScopeRootPartition(scopeRootId),
                           StoragePartitionQuery.All(),
                           cancellationToken).ConfigureAwait(false))
        {
            yield return item.Value.Tenant;
        }
    }

    public async IAsyncEnumerable<Tenant> GetAccessibleTenantsAsync(
        AccessUserId userId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var item in storage.AccessibleTenantsByUser.QueryPartitionAsync(
                           AccessStorageKeys.AccessibleTenantByUserPartition(userId),
                           StoragePartitionQuery.All(),
                           cancellationToken).ConfigureAwait(false))
        {
            yield return item.Value.Tenant;
        }
    }

    public async IAsyncEnumerable<AccessAssignment> GetAssignmentsForUserAsync(
        AccessUserId userId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var item in storage.AssignmentsByUser.QueryPartitionAsync(
                           AccessStorageKeys.AssignmentByUserPartition(userId),
                           StoragePartitionQuery.All(),
                           cancellationToken).ConfigureAwait(false))
        {
            yield return item.Value.Assignment;
        }
    }

    public async IAsyncEnumerable<AccessAssignment> GetAssignmentsForResourceAsync(
        AccessResourceReference resource,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(resource);

        await foreach (var item in storage.AssignmentsByResource.QueryPartitionAsync(
                           AccessStorageKeys.AssignmentByResourcePartition(resource),
                           StoragePartitionQuery.All(),
                           cancellationToken).ConfigureAwait(false))
        {
            yield return item.Value.Assignment;
        }
    }

    public async IAsyncEnumerable<ExplicitPermissionGrant> GetExplicitPermissionGrantsForUserAsync(
        AccessUserId userId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var item in storage.GrantsByUser.QueryPartitionAsync(
                           AccessStorageKeys.GrantByUserPartition(userId),
                           StoragePartitionQuery.All(),
                           cancellationToken).ConfigureAwait(false))
        {
            yield return item.Value.Grant;
        }
    }

    public async IAsyncEnumerable<ExplicitPermissionGrant> GetExplicitPermissionGrantsForResourceAsync(
        AccessResourceReference resource,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(resource);

        await foreach (var item in storage.GrantsByResource.QueryPartitionAsync(
                           AccessStorageKeys.GrantByResourcePartition(resource),
                           StoragePartitionQuery.All(),
                           cancellationToken).ConfigureAwait(false))
        {
            yield return item.Value.Grant;
        }
    }
}
