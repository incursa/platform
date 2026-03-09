namespace Incursa.Integrations.WorkOS.Access;

using Incursa.Platform.Access;

public sealed record WorkOsAccessSyncRequest
{
    public WorkOsAccessSyncRequest(
        AccessUser user,
        IReadOnlyCollection<WorkOsOrganizationMembership>? memberships = null,
        bool enqueueReconciliationWorkItem = false,
        string? reconciliationDetail = null)
    {
        ArgumentNullException.ThrowIfNull(user);

        User = user;
        Memberships = memberships?.ToArray() ?? Array.Empty<WorkOsOrganizationMembership>();
        EnqueueReconciliationWorkItem = enqueueReconciliationWorkItem;
        ReconciliationDetail = string.IsNullOrWhiteSpace(reconciliationDetail) ? null : reconciliationDetail.Trim();
    }

    public AccessUser User { get; }

    public IReadOnlyCollection<WorkOsOrganizationMembership> Memberships { get; }

    public bool EnqueueReconciliationWorkItem { get; }

    public string? ReconciliationDetail { get; }
}
