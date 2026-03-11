namespace Incursa.Integrations.WorkOS.Abstractions.Authentication;

public interface IWorkOsPasswordResetClient
{
    Task<WorkOsPasswordReset> CreatePasswordResetAsync(
        WorkOsPasswordResetCreateRequest request,
        CancellationToken cancellationToken = default);

    Task ResetPasswordAsync(
        WorkOsPasswordResetConfirmRequest request,
        CancellationToken cancellationToken = default);
}
