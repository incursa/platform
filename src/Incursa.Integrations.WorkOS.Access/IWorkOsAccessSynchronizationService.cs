namespace Incursa.Integrations.WorkOS.Access;

public interface IWorkOsAccessSynchronizationService
{
    Task<WorkOsAccessSyncResult> SynchronizeAsync(
        WorkOsAccessSyncRequest request,
        CancellationToken cancellationToken = default);
}
