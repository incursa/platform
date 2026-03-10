namespace Incursa.Integrations.WorkOS.Abstractions.Authentication;

public interface IWorkOsSessionClient
{
    Task<WorkOsSessionSignOutResult> SignOutAsync(
        WorkOsSignOutRequest request,
        CancellationToken cancellationToken = default);
}
