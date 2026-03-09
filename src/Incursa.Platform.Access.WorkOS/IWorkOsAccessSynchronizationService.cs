namespace Incursa.Platform.Access.WorkOS;

public interface IWorkOsAccessSynchronizationService
{
    Task<WorkOsAccessSyncResult> SynchronizeAsync(
        WorkOsAccessSyncRequest request,
        CancellationToken cancellationToken = default);
}
