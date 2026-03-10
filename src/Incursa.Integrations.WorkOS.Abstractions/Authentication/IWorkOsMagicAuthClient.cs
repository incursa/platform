namespace Incursa.Integrations.WorkOS.Abstractions.Authentication;

public interface IWorkOsMagicAuthClient
{
    Task<WorkOsMagicAuthStartResult> BeginAsync(
        WorkOsMagicAuthStartRequest request,
        CancellationToken cancellationToken = default);
}
