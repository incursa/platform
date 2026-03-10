namespace Incursa.Integrations.WorkOS.Access;

using Incursa.Integrations.WorkOS.Abstractions.Authentication;
using Incursa.Platform.Access;

internal sealed class WorkOsAccessAuthenticationService : IAccessAuthenticationService
{
    private readonly IWorkOsAuthenticationClient authenticationClient;
    private readonly IWorkOsMagicAuthClient magicAuthClient;
    private readonly IWorkOsSessionClient sessionClient;
    private readonly IWorkOsTokenValidator tokenValidator;

    public WorkOsAccessAuthenticationService(
        IWorkOsAuthenticationClient authenticationClient,
        IWorkOsMagicAuthClient magicAuthClient,
        IWorkOsSessionClient sessionClient,
        IWorkOsTokenValidator tokenValidator)
    {
        this.authenticationClient = authenticationClient ?? throw new ArgumentNullException(nameof(authenticationClient));
        this.magicAuthClient = magicAuthClient ?? throw new ArgumentNullException(nameof(magicAuthClient));
        this.sessionClient = sessionClient ?? throw new ArgumentNullException(nameof(sessionClient));
        this.tokenValidator = tokenValidator ?? throw new ArgumentNullException(nameof(tokenValidator));
    }

    public async Task<AccessRedirectAuthorization> CreateAuthorizationUrlAsync(
        AccessRedirectAuthorizationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var url = await authenticationClient.CreateAuthorizationUrlAsync(
            new WorkOsAuthorizationRequest(
                request.RedirectUri,
                request.Provider,
                request.ConnectionId,
                request.OrganizationId,
                request.State,
                request.CodeChallenge,
                request.CodeChallengeMethod,
                request.LoginHint,
                request.DomainHint,
                request.ScreenHint,
                request.ProviderScopes,
                request.AdditionalParameters),
            cancellationToken).ConfigureAwait(false);

        return new AccessRedirectAuthorization(url);
    }

    public Task<AccessAuthenticationOutcome> ExchangeCodeAsync(
        AccessCodeExchangeRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return MapAsync(
            authenticationClient.ExchangeCodeAsync(
                new WorkOsCodeExchangeRequest(
                    request.Code,
                    request.RedirectUri ?? throw new ArgumentException("Redirect uri is required.", nameof(request)),
                    request.CodeVerifier,
                    request.InvitationToken,
                    Map(request.Metadata)),
                cancellationToken),
            cancellationToken);
    }

    public Task<AccessAuthenticationOutcome> SignInWithPasswordAsync(
        AccessPasswordSignInRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return MapAsync(
            authenticationClient.AuthenticateWithPasswordAsync(
                new WorkOsPasswordAuthenticationRequest(
                    request.Email,
                    request.Password,
                    Map(request.Metadata)),
                cancellationToken),
            cancellationToken);
    }

    public async Task<AccessMagicAuthStartResult> BeginMagicAuthAsync(
        AccessMagicAuthStartRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var result = await magicAuthClient.BeginAsync(
            new WorkOsMagicAuthStartRequest(request.Email, request.ReturnCode),
            cancellationToken).ConfigureAwait(false);

        return new AccessMagicAuthStartResult(
            result.Id,
            result.Email,
            result.ExpiresAtUtc,
            result.Code,
            true);
    }

    public async Task<AccessTotpEnrollment> EnrollTotpAsync(
        AccessTotpEnrollmentRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var enrollment = await authenticationClient.EnrollTotpAsync(
            new WorkOsTotpEnrollmentRequest(request.User, request.Issuer),
            cancellationToken).ConfigureAwait(false);

        return new AccessTotpEnrollment(
            enrollment.FactorId,
            enrollment.Issuer ?? request.Issuer,
            enrollment.User ?? request.User,
            enrollment.QrCode,
            enrollment.Secret,
            enrollment.Uri);
    }

    public Task<AccessAuthenticationOutcome> CompleteMagicAuthAsync(
        AccessMagicAuthCompletionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return MapAsync(
            authenticationClient.CompleteMagicAuthAsync(
                new WorkOsMagicAuthCompletionRequest(
                    request.Code,
                    Map(request.Metadata)),
                cancellationToken),
            cancellationToken);
    }

    public Task<AccessAuthenticationOutcome> CompleteEmailVerificationAsync(
        AccessEmailVerificationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return MapAsync(
            authenticationClient.CompleteEmailVerificationAsync(
                new WorkOsEmailVerificationRequest(
                    request.PendingAuthenticationToken,
                    request.Code,
                    request.EmailVerificationId,
                    Map(request.Metadata)),
                cancellationToken),
            cancellationToken);
    }

    public Task<AccessAuthenticationOutcome> CompleteTotpAsync(
        AccessTotpCompletionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.Code))
        {
            throw new ArgumentException("Code is required.", nameof(request));
        }

        return MapAsync(
            authenticationClient.CompleteTotpAsync(
                new WorkOsTotpAuthenticationRequest(
                    request.PendingAuthenticationToken,
                    request.AuthenticationFactorId,
                    request.AuthenticationChallengeId,
                    request.Code,
                    request.EnrollmentIssuer,
                    request.EnrollmentUser,
                    Map(request.Metadata)),
                cancellationToken),
            cancellationToken);
    }

    public Task<AccessAuthenticationOutcome> CompleteOrganizationSelectionAsync(
        AccessOrganizationSelectionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return MapAsync(
            authenticationClient.CompleteOrganizationSelectionAsync(
                new WorkOsOrganizationSelectionRequest(
                    request.PendingAuthenticationToken,
                    request.OrganizationId,
                    Map(request.Metadata)),
                cancellationToken),
            cancellationToken);
    }

    public Task<AccessAuthenticationOutcome> RefreshAsync(
        AccessRefreshRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return MapAsync(
            authenticationClient.RefreshAsync(
                new WorkOsRefreshRequest(
                    request.RefreshToken,
                    request.OrganizationId,
                    Map(request.Metadata)),
                cancellationToken),
            cancellationToken);
    }

    public async Task<AccessSignOutResult> SignOutAsync(
        AccessSignOutRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var result = await sessionClient.SignOutAsync(
            new WorkOsSignOutRequest(request.SessionId, request.ReturnToUri),
            cancellationToken).ConfigureAwait(false);

        return new AccessSignOutResult(result.SessionRevoked, result.LogoutUrl);
    }

    private async Task<AccessAuthenticationOutcome> MapAsync(
        Task<WorkOsAuthenticationResult> resultTask,
        CancellationToken cancellationToken)
    {
        var result = await resultTask.ConfigureAwait(false);
        return result switch
        {
            WorkOsAuthenticationSuccess success => await MapSuccessAsync(success.Session, cancellationToken).ConfigureAwait(false),
            WorkOsAuthenticationChallenge challenge => new AccessAuthenticationChallengeRequired(Map(challenge)),
            WorkOsAuthenticationFailure failure => new AccessAuthenticationFailed(
                new AccessFailure(
                    failure.Failure.Code,
                    failure.Failure.Message,
                    failure.Failure.IsTransient)),
            _ => throw new InvalidOperationException("Unknown WorkOS authentication result type."),
        };
    }

    private async Task<AccessAuthenticationOutcome> MapSuccessAsync(
        WorkOsAuthenticatedSession session,
        CancellationToken cancellationToken)
    {
        var (mappedSession, validationFailure) = await MapSessionAsync(session, cancellationToken).ConfigureAwait(false);
        return mappedSession is null
            ? new AccessAuthenticationFailed(validationFailure ?? new AccessFailure("invalid_token", "The WorkOS access token could not be validated."))
            : new AccessAuthenticationSucceeded(mappedSession);
    }

    private async Task<(AccessAuthenticatedSession? Session, AccessFailure? Failure)> MapSessionAsync(
        WorkOsAuthenticatedSession session,
        CancellationToken cancellationToken)
    {
        var validation = string.IsNullOrWhiteSpace(session.AccessToken)
            ? new WorkOsTokenValidationResult(false, FailureCode: "missing_access_token", FailureMessage: "The WorkOS response did not include an access token.")
            : await tokenValidator.ValidateAsync(session.AccessToken!, cancellationToken).ConfigureAwait(false);

        if (!validation.IsValid || validation.Claims is null)
        {
            return (null, new AccessFailure(
                validation.FailureCode ?? "invalid_token",
                validation.FailureMessage ?? "The WorkOS access token could not be validated."));
        }

        var accessContext = validation.Claims;
        return (new AccessAuthenticatedSession(
            accessContext.SubjectId,
            session.AccessToken!,
            session.RefreshToken,
            accessContext.SessionId,
            accessContext.OrganizationId,
            accessContext.Roles,
            accessContext.Permissions,
            accessContext.FeatureFlags,
            accessContext.Entitlements,
            session.Email,
            session.DisplayName,
            session.EmailVerified,
            accessTokenExpiresAtUtc: accessContext.ExpiresAtUtc,
            refreshTokenExpiresAtUtc: session.RefreshTokenExpiresAtUtc),
            null);
    }

    private static AccessChallenge Map(WorkOsAuthenticationChallenge challenge) =>
        new(
            Map(challenge.Kind),
            challenge.PendingAuthentication.PendingAuthenticationToken,
            challenge.Code,
            challenge.Message,
            challenge.PendingAuthentication.Email,
            challenge.PendingAuthentication.EmailVerificationId,
            challenge.PendingAuthentication.AuthenticationChallengeId,
            challenge.PendingAuthentication.Factors.Select(Map).ToArray(),
            challenge.PendingAuthentication.Organizations.Select(static item =>
                new AccessOrganizationChoice(item.Id, item.Name)).ToArray(),
            challenge.PendingAuthentication.TotpEnrollment is null
                ? null
                : new AccessTotpEnrollment(
                    challenge.PendingAuthentication.TotpEnrollment.FactorId,
                    challenge.PendingAuthentication.TotpEnrollment.Issuer,
                    challenge.PendingAuthentication.TotpEnrollment.User,
                    challenge.PendingAuthentication.TotpEnrollment.QrCode,
                    challenge.PendingAuthentication.TotpEnrollment.Secret,
                    challenge.PendingAuthentication.TotpEnrollment.Uri));

    private static AccessAuthenticationFactor Map(WorkOsAuthenticationFactor factor) =>
        new(factor.Id, factor.Type);

    private static AccessChallengeKind Map(WorkOsChallengeKind kind) =>
        kind switch
        {
            WorkOsChallengeKind.EmailVerificationRequired => AccessChallengeKind.EmailVerificationRequired,
            WorkOsChallengeKind.MfaEnrollmentRequired => AccessChallengeKind.MfaEnrollmentRequired,
            WorkOsChallengeKind.MfaChallengeRequired => AccessChallengeKind.MfaChallengeRequired,
            WorkOsChallengeKind.OrganizationSelectionRequired => AccessChallengeKind.OrganizationSelectionRequired,
            WorkOsChallengeKind.IdentityLinkingRequired => AccessChallengeKind.IdentityLinkingRequired,
            _ => AccessChallengeKind.ProviderChallengeRequired,
        };

    private static WorkOsRequestMetadata? Map(AccessAuthenticationRequestMetadata? metadata) =>
        metadata is null ? null : new WorkOsRequestMetadata(metadata.IpAddress, metadata.UserAgent);
}
