namespace Incursa.Integrations.WorkOS.Abstractions.Authentication;

public interface IWorkOsAuthenticationClient
{
    Task<Uri> CreateAuthorizationUrlAsync(
        WorkOsAuthorizationRequest request,
        CancellationToken cancellationToken = default);

    Task<WorkOsAuthenticationResult> ExchangeCodeAsync(
        WorkOsCodeExchangeRequest request,
        CancellationToken cancellationToken = default);

    Task<WorkOsAuthenticationResult> AuthenticateWithPasswordAsync(
        WorkOsPasswordAuthenticationRequest request,
        CancellationToken cancellationToken = default);

    Task<WorkOsAuthenticationResult> CompleteMagicAuthAsync(
        WorkOsMagicAuthCompletionRequest request,
        CancellationToken cancellationToken = default);

    Task<WorkOsAuthenticationResult> CompleteEmailVerificationAsync(
        WorkOsEmailVerificationRequest request,
        CancellationToken cancellationToken = default);

    Task<WorkOsAuthenticationResult> CompleteTotpAsync(
        WorkOsTotpAuthenticationRequest request,
        CancellationToken cancellationToken = default);

    Task<WorkOsAuthenticationResult> CompleteOrganizationSelectionAsync(
        WorkOsOrganizationSelectionRequest request,
        CancellationToken cancellationToken = default);

    Task<WorkOsAuthenticationResult> RefreshAsync(
        WorkOsRefreshRequest request,
        CancellationToken cancellationToken = default);

    Task<WorkOsTotpEnrollment> EnrollTotpAsync(
        WorkOsTotpEnrollmentRequest request,
        CancellationToken cancellationToken = default);
}
