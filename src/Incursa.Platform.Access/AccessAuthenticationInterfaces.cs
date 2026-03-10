#pragma warning disable MA0048
namespace Incursa.Platform.Access;

public interface IAccessAuthenticationService
{
    Task<AccessRedirectAuthorization> CreateAuthorizationUrlAsync(
        AccessRedirectAuthorizationRequest request,
        CancellationToken cancellationToken = default);

    Task<AccessAuthenticationOutcome> ExchangeCodeAsync(
        AccessCodeExchangeRequest request,
        CancellationToken cancellationToken = default);

    Task<AccessAuthenticationOutcome> SignInWithPasswordAsync(
        AccessPasswordSignInRequest request,
        CancellationToken cancellationToken = default);

    Task<AccessMagicAuthStartResult> BeginMagicAuthAsync(
        AccessMagicAuthStartRequest request,
        CancellationToken cancellationToken = default);

    Task<AccessTotpEnrollment> EnrollTotpAsync(
        AccessTotpEnrollmentRequest request,
        CancellationToken cancellationToken = default);

    Task<AccessAuthenticationOutcome> CompleteMagicAuthAsync(
        AccessMagicAuthCompletionRequest request,
        CancellationToken cancellationToken = default);

    Task<AccessAuthenticationOutcome> CompleteEmailVerificationAsync(
        AccessEmailVerificationRequest request,
        CancellationToken cancellationToken = default);

    Task<AccessAuthenticationOutcome> CompleteTotpAsync(
        AccessTotpCompletionRequest request,
        CancellationToken cancellationToken = default);

    Task<AccessAuthenticationOutcome> CompleteOrganizationSelectionAsync(
        AccessOrganizationSelectionRequest request,
        CancellationToken cancellationToken = default);

    Task<AccessAuthenticationOutcome> RefreshAsync(
        AccessRefreshRequest request,
        CancellationToken cancellationToken = default);

    Task<AccessSignOutResult> SignOutAsync(
        AccessSignOutRequest request,
        CancellationToken cancellationToken = default);
}

public interface IAccessSessionStore
{
    Task<AccessAuthenticatedSession?> GetAsync(CancellationToken cancellationToken = default);

    Task SetAsync(AccessAuthenticatedSession session, CancellationToken cancellationToken = default);

    Task ClearAsync(CancellationToken cancellationToken = default);
}
#pragma warning restore MA0048
