namespace Incursa.Platform.Access.Internal;

using System.Globalization;
using Incursa.Platform.Storage;

internal static class AccessStorageKeys
{
    public static StorageRecordKey User(AccessUserId userId) =>
        new(new StoragePartitionKey("access-user"), new StorageRowKey(Safe(userId.Value)));

    public static StorageRecordKey ScopeRoot(ScopeRootId scopeRootId) =>
        new(new StoragePartitionKey("access-scope-root"), new StorageRowKey(Safe(scopeRootId.Value)));

    public static StorageRecordKey Tenant(TenantId tenantId) =>
        new(new StoragePartitionKey("access-tenant"), new StorageRowKey(Safe(tenantId.Value)));

    public static StorageRecordKey Membership(ScopeMembershipId membershipId) =>
        new(new StoragePartitionKey("access-membership"), new StorageRowKey(Safe(membershipId.Value)));

    public static StorageRecordKey Assignment(AccessAssignmentId assignmentId) =>
        new(new StoragePartitionKey("access-assignment"), new StorageRowKey(Safe(assignmentId.Value)));

    public static StorageRecordKey Grant(ExplicitPermissionGrantId grantId) =>
        new(new StoragePartitionKey("access-grant"), new StorageRowKey(Safe(grantId.Value)));

    public static StorageRecordKey AuditEntry(AccessAuditEntryId entryId) =>
        new(new StoragePartitionKey("access-audit"), new StorageRowKey(Safe(entryId.Value)));

    public static StoragePartitionKey MembershipByUserPartition(AccessUserId userId) =>
        new("access-membership-by-user/" + Safe(userId.Value));

    public static StorageRecordKey MembershipByUser(AccessUserId userId, ScopeMembershipId membershipId) =>
        new(MembershipByUserPartition(userId), new StorageRowKey(Safe(membershipId.Value)));

    public static StoragePartitionKey MembershipByScopeRootPartition(ScopeRootId scopeRootId) =>
        new("access-membership-by-scope-root/" + Safe(scopeRootId.Value));

    public static StorageRecordKey MembershipByScopeRoot(ScopeRootId scopeRootId, ScopeMembershipId membershipId) =>
        new(MembershipByScopeRootPartition(scopeRootId), new StorageRowKey(Safe(membershipId.Value)));

    public static StoragePartitionKey TenantByScopeRootPartition(ScopeRootId scopeRootId) =>
        new("access-tenant-by-scope-root/" + Safe(scopeRootId.Value));

    public static StorageRecordKey TenantByScopeRoot(ScopeRootId scopeRootId, TenantId tenantId) =>
        new(TenantByScopeRootPartition(scopeRootId), new StorageRowKey(Safe(tenantId.Value)));

    public static StoragePartitionKey AccessibleTenantByUserPartition(AccessUserId userId) =>
        new("access-accessible-tenant-by-user/" + Safe(userId.Value));

    public static StorageRecordKey AccessibleTenantByUser(AccessUserId userId, TenantId tenantId) =>
        new(AccessibleTenantByUserPartition(userId), new StorageRowKey(Safe(tenantId.Value)));

    public static StoragePartitionKey AssignmentByUserPartition(AccessUserId userId) =>
        new("access-assignment-by-user/" + Safe(userId.Value));

    public static StorageRecordKey AssignmentByUser(AccessUserId userId, AccessAssignmentId assignmentId) =>
        new(AssignmentByUserPartition(userId), new StorageRowKey(Safe(assignmentId.Value)));

    public static StoragePartitionKey AssignmentByResourcePartition(AccessResourceReference resource) =>
        new("access-assignment-by-resource/" + Safe(resource.ToString()));

    public static StorageRecordKey AssignmentByResource(AccessResourceReference resource, AccessAssignmentId assignmentId) =>
        new(AssignmentByResourcePartition(resource), new StorageRowKey(Safe(assignmentId.Value)));

    public static StoragePartitionKey GrantByUserPartition(AccessUserId userId) =>
        new("access-grant-by-user/" + Safe(userId.Value));

    public static StorageRecordKey GrantByUser(AccessUserId userId, ExplicitPermissionGrantId grantId) =>
        new(GrantByUserPartition(userId), new StorageRowKey(Safe(grantId.Value)));

    public static StoragePartitionKey GrantByResourcePartition(AccessResourceReference resource) =>
        new("access-grant-by-resource/" + Safe(resource.ToString()));

    public static StorageRecordKey GrantByResource(AccessResourceReference resource, ExplicitPermissionGrantId grantId) =>
        new(GrantByResourcePartition(resource), new StorageRowKey(Safe(grantId.Value)));

    public static StoragePartitionKey AuditByUserPartition(AccessUserId userId) =>
        new("access-audit-by-user/" + Safe(userId.Value));

    public static StorageRecordKey AuditByUser(
        AccessUserId userId,
        DateTimeOffset occurredUtc,
        AccessAuditEntryId entryId) =>
        new(AuditByUserPartition(userId), new StorageRowKey(AuditLookupRow(occurredUtc, entryId)));

    public static StoragePartitionKey AuditByResourcePartition(AccessResourceReference resource) =>
        new("access-audit-by-resource/" + Safe(resource.ToString()));

    public static StorageRecordKey AuditByResource(
        AccessResourceReference resource,
        DateTimeOffset occurredUtc,
        AccessAuditEntryId entryId) =>
        new(AuditByResourcePartition(resource), new StorageRowKey(AuditLookupRow(occurredUtc, entryId)));

    public static string AuditLookupRow(DateTimeOffset occurredUtc, AccessAuditEntryId entryId) =>
        occurredUtc.UtcDateTime.ToString("O", CultureInfo.InvariantCulture) + "|" + Safe(entryId.Value);

    public static StorageRecordKey ScopeRootByExternalLink(string provider, string externalId, string? resourceType) =>
        new(
            new StoragePartitionKey("access-scope-root-by-external-link/" + NormalizeProvider(provider) + "/" + NormalizeResourceType(resourceType)),
            new StorageRowKey(NormalizeExternalId(externalId)));

    public static StorageRecordKey TenantByExternalLink(string provider, string externalId, string? resourceType) =>
        new(
            new StoragePartitionKey("access-tenant-by-external-link/" + NormalizeProvider(provider) + "/" + NormalizeResourceType(resourceType)),
            new StorageRowKey(NormalizeExternalId(externalId)));

    private static string NormalizeProvider(string provider)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);
        return provider.Trim().ToUpperInvariant();
    }

    private static string NormalizeResourceType(string? resourceType) =>
        string.IsNullOrWhiteSpace(resourceType) ? "DEFAULT" : resourceType.Trim().ToUpperInvariant();

    private static string NormalizeExternalId(string externalId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(externalId);
        return Safe(externalId);
    }

    private static string Safe(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return Uri.EscapeDataString(value.Trim());
    }
}
