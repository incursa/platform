namespace Incursa.Integrations.WorkOS.AspNetCore.Auth;

using Incursa.Integrations.WorkOS.Abstractions.Authentication;
using Incursa.Platform.Access.Razor;

internal sealed class WorkOsAccessPasswordRecoveryService : IAccessPasswordRecoveryService
{
    private readonly IWorkOsPasswordResetClient passwordResetClient;

    public WorkOsAccessPasswordRecoveryService(IWorkOsPasswordResetClient passwordResetClient)
    {
        this.passwordResetClient = passwordResetClient ?? throw new ArgumentNullException(nameof(passwordResetClient));
    }

    public async Task<AccessPasswordRecoveryResult> RequestResetAsync(
        AccessPasswordResetRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await passwordResetClient
            .CreatePasswordResetAsync(new WorkOsPasswordResetCreateRequest(request.Email), cancellationToken)
            .ConfigureAwait(false);

        return new AccessPasswordRecoveryResult(true, "If the account exists, a password reset email is on the way.");
    }

    public async Task<AccessPasswordRecoveryResult> ResetPasswordAsync(
        AccessPasswordResetCompletionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await passwordResetClient
            .ResetPasswordAsync(
                new WorkOsPasswordResetConfirmRequest(request.Token, request.NewPassword),
                cancellationToken)
            .ConfigureAwait(false);

        return new AccessPasswordRecoveryResult(true, "Password updated. Sign in with your new password.");
    }
}
