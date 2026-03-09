namespace Incursa.Platform.Access.Internal;

using Incursa.Platform.Storage;

internal sealed class AccessStorageContext
{
    public AccessStorageContext(
        IRecordStore<AccessUser> users,
        IRecordStore<ScopeRoot> scopeRoots,
        IRecordStore<Tenant> tenants,
        IRecordStore<ScopeMembership> memberships,
        IRecordStore<AccessAssignment> assignments,
        IRecordStore<ExplicitPermissionGrant> grants,
        IRecordStore<AccessAuditEntry> auditEntries,
        ILookupStore<MembershipByUserProjection> membershipsByUser,
        ILookupStore<MembershipByScopeRootProjection> membershipsByScopeRoot,
        ILookupStore<TenantByScopeRootProjection> tenantsByScopeRoot,
        ILookupStore<AccessibleTenantByUserProjection> accessibleTenantsByUser,
        ILookupStore<AssignmentByUserProjection> assignmentsByUser,
        ILookupStore<AssignmentByResourceProjection> assignmentsByResource,
        ILookupStore<GrantByUserProjection> grantsByUser,
        ILookupStore<GrantByResourceProjection> grantsByResource,
        ILookupStore<AuditEntryByUserProjection> auditEntriesByUser,
        ILookupStore<AuditEntryByResourceProjection> auditEntriesByResource,
        ILookupStore<ScopeRootByExternalLinkProjection> scopeRootsByExternalLink,
        ILookupStore<TenantByExternalLinkProjection> tenantsByExternalLink,
        IWorkStore<AccessWorkItem> workItems,
        ICoordinationStore coordination)
    {
        Users = users;
        ScopeRoots = scopeRoots;
        Tenants = tenants;
        Memberships = memberships;
        Assignments = assignments;
        Grants = grants;
        AuditEntries = auditEntries;
        MembershipsByUser = membershipsByUser;
        MembershipsByScopeRoot = membershipsByScopeRoot;
        TenantsByScopeRoot = tenantsByScopeRoot;
        AccessibleTenantsByUser = accessibleTenantsByUser;
        AssignmentsByUser = assignmentsByUser;
        AssignmentsByResource = assignmentsByResource;
        GrantsByUser = grantsByUser;
        GrantsByResource = grantsByResource;
        AuditEntriesByUser = auditEntriesByUser;
        AuditEntriesByResource = auditEntriesByResource;
        ScopeRootsByExternalLink = scopeRootsByExternalLink;
        TenantsByExternalLink = tenantsByExternalLink;
        WorkItems = workItems;
        Coordination = coordination;
    }

    public IRecordStore<AccessUser> Users { get; }

    public IRecordStore<ScopeRoot> ScopeRoots { get; }

    public IRecordStore<Tenant> Tenants { get; }

    public IRecordStore<ScopeMembership> Memberships { get; }

    public IRecordStore<AccessAssignment> Assignments { get; }

    public IRecordStore<ExplicitPermissionGrant> Grants { get; }

    public IRecordStore<AccessAuditEntry> AuditEntries { get; }

    public ILookupStore<MembershipByUserProjection> MembershipsByUser { get; }

    public ILookupStore<MembershipByScopeRootProjection> MembershipsByScopeRoot { get; }

    public ILookupStore<TenantByScopeRootProjection> TenantsByScopeRoot { get; }

    public ILookupStore<AccessibleTenantByUserProjection> AccessibleTenantsByUser { get; }

    public ILookupStore<AssignmentByUserProjection> AssignmentsByUser { get; }

    public ILookupStore<AssignmentByResourceProjection> AssignmentsByResource { get; }

    public ILookupStore<GrantByUserProjection> GrantsByUser { get; }

    public ILookupStore<GrantByResourceProjection> GrantsByResource { get; }

    public ILookupStore<AuditEntryByUserProjection> AuditEntriesByUser { get; }

    public ILookupStore<AuditEntryByResourceProjection> AuditEntriesByResource { get; }

    public ILookupStore<ScopeRootByExternalLinkProjection> ScopeRootsByExternalLink { get; }

    public ILookupStore<TenantByExternalLinkProjection> TenantsByExternalLink { get; }

    public IWorkStore<AccessWorkItem> WorkItems { get; }

    public ICoordinationStore Coordination { get; }
}
