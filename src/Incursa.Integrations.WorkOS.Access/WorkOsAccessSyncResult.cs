namespace Incursa.Integrations.WorkOS.Access;

using Incursa.Platform.Access;

public sealed record WorkOsAccessSyncResult
{
    public WorkOsAccessSyncResult(
        AccessUser user,
        IReadOnlyCollection<ScopeRoot>? scopeRoots = null,
        IReadOnlyCollection<ScopeMembership>? memberships = null,
        IReadOnlyCollection<AccessAssignment>? assignments = null,
        IReadOnlyCollection<ScopeMembershipId>? deletedMembershipIds = null,
        IReadOnlyCollection<AccessAssignmentId>? deletedAssignmentIds = null,
        IReadOnlyCollection<string>? unmappedRoleSlugs = null,
        AccessWorkItem? reconciliationWorkItem = null)
    {
        ArgumentNullException.ThrowIfNull(user);

        User = user;
        ScopeRoots = scopeRoots?.ToArray() ?? Array.Empty<ScopeRoot>();
        Memberships = memberships?.ToArray() ?? Array.Empty<ScopeMembership>();
        Assignments = assignments?.ToArray() ?? Array.Empty<AccessAssignment>();
        DeletedMembershipIds = deletedMembershipIds?.ToArray() ?? Array.Empty<ScopeMembershipId>();
        DeletedAssignmentIds = deletedAssignmentIds?.ToArray() ?? Array.Empty<AccessAssignmentId>();
        UnmappedRoleSlugs = unmappedRoleSlugs?.ToArray() ?? Array.Empty<string>();
        ReconciliationWorkItem = reconciliationWorkItem;
    }

    public AccessUser User { get; }

    public IReadOnlyCollection<ScopeRoot> ScopeRoots { get; }

    public IReadOnlyCollection<ScopeMembership> Memberships { get; }

    public IReadOnlyCollection<AccessAssignment> Assignments { get; }

    public IReadOnlyCollection<ScopeMembershipId> DeletedMembershipIds { get; }

    public IReadOnlyCollection<AccessAssignmentId> DeletedAssignmentIds { get; }

    public IReadOnlyCollection<string> UnmappedRoleSlugs { get; }

    public AccessWorkItem? ReconciliationWorkItem { get; }
}
