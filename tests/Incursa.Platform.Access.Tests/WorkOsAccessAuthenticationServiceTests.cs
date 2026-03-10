namespace Incursa.Platform.Access.Tests;

using Incursa.Integrations.WorkOS.Abstractions.Authentication;
using Incursa.Integrations.WorkOS.Access;

[Trait("Category", "Unit")]
public sealed class WorkOsAccessAuthenticationServiceTests
{
    [Fact]
    public async Task SignInWithPasswordAsync_EmailVerificationChallenge_MapsNormalizedChallengeAsync()
    {
        var service = CreateService(CreateChallenge(
            WorkOsChallengeKind.EmailVerificationRequired,
            emailVerificationId: "email_ver_123"));

        var outcome = await service.SignInWithPasswordAsync(
            new AccessPasswordSignInRequest("ada@example.com", "not-a-real-password"),
            TestContext.Current.CancellationToken);

        var challenge = outcome.ShouldBeOfType<AccessAuthenticationChallengeRequired>().Challenge;
        challenge.Kind.ShouldBe(AccessChallengeKind.EmailVerificationRequired);
        challenge.PendingAuthenticationToken.ShouldBe("pending_123");
        challenge.Email.ShouldBe("ada@example.com");
        challenge.EmailVerificationId.ShouldBe("email_ver_123");
        challenge.Message.ShouldBe("Challenge required.");
    }

    [Fact]
    public async Task SignInWithPasswordAsync_MfaEnrollmentChallenge_MapsTotpEnrollmentAsync()
    {
        var service = CreateService(CreateChallenge(
            WorkOsChallengeKind.MfaEnrollmentRequired,
            totpEnrollment: new WorkOsTotpEnrollment(
                "factor_123",
                "Incursa",
                "ada@example.com",
                "data:image/png;base64,qr",
                "secret-123",
                "otpauth://totp/Incursa:ada@example.com")));

        var outcome = await service.SignInWithPasswordAsync(
            new AccessPasswordSignInRequest("ada@example.com", "password"),
            TestContext.Current.CancellationToken);

        var challenge = outcome.ShouldBeOfType<AccessAuthenticationChallengeRequired>().Challenge;
        challenge.Kind.ShouldBe(AccessChallengeKind.MfaEnrollmentRequired);
        challenge.TotpEnrollment.ShouldNotBeNull();
        challenge.TotpEnrollment.FactorId.ShouldBe("factor_123");
        challenge.TotpEnrollment.Secret.ShouldBe("secret-123");
        challenge.TotpEnrollment.Uri.ShouldBe("otpauth://totp/Incursa:ada@example.com");
    }

    [Fact]
    public async Task SignInWithPasswordAsync_MfaChallenge_MapsFactorsAndChallengeIdAsync()
    {
        var service = CreateService(CreateChallenge(
            WorkOsChallengeKind.MfaChallengeRequired,
            authenticationChallengeId: "auth_challenge_123",
            factors:
            [
                new WorkOsAuthenticationFactor("factor_123", "totp"),
            ]));

        var outcome = await service.SignInWithPasswordAsync(
            new AccessPasswordSignInRequest("ada@example.com", "password"),
            TestContext.Current.CancellationToken);

        var challenge = outcome.ShouldBeOfType<AccessAuthenticationChallengeRequired>().Challenge;
        challenge.Kind.ShouldBe(AccessChallengeKind.MfaChallengeRequired);
        challenge.AuthenticationChallengeId.ShouldBe("auth_challenge_123");
        challenge.Factors.ShouldHaveSingleItem();
        challenge.Factors.Single().Id.ShouldBe("factor_123");
        challenge.Factors.Single().Type.ShouldBe("totp");
    }

    [Fact]
    public async Task SignInWithPasswordAsync_OrganizationSelectionChallenge_MapsOrganizationsAsync()
    {
        var service = CreateService(CreateChallenge(
            WorkOsChallengeKind.OrganizationSelectionRequired,
            organizations:
            [
                new WorkOsOrganizationChoice("org_1", "Northwind"),
                new WorkOsOrganizationChoice("org_2", "Contoso"),
            ]));

        var outcome = await service.SignInWithPasswordAsync(
            new AccessPasswordSignInRequest("ada@example.com", "password"),
            TestContext.Current.CancellationToken);

        var challenge = outcome.ShouldBeOfType<AccessAuthenticationChallengeRequired>().Challenge;
        challenge.Kind.ShouldBe(AccessChallengeKind.OrganizationSelectionRequired);
        challenge.Organizations.Select(static item => item.Id).ShouldBe(["org_1", "org_2"]);
        challenge.Organizations.Select(static item => item.Name).ShouldBe(["Northwind", "Contoso"]);
    }

    [Fact]
    public async Task RefreshAsync_MapsRotatedRefreshTokenAndClaimsAsync()
    {
        var accessTokenClaims = new WorkOsTokenClaims(
            "user_123",
            "session_123",
            "org_456",
            ["admin", "owner"],
            ["audit:read", "users:write"],
            ["beta-dashboard"],
            ["tier:pro"],
            DateTimeOffset.UtcNow.AddMinutes(10));
        var session = new WorkOsAuthenticatedSession(
            "user_123",
            "access-token-rotated",
            accessTokenClaims,
            "refresh-token-rotated",
            "ada@example.com",
            "Ada Lovelace",
            true,
            accessTokenExpiresAtUtc: accessTokenClaims.ExpiresAtUtc,
            refreshTokenExpiresAtUtc: DateTimeOffset.UtcNow.AddDays(30));
        var authenticationClient = new StubWorkOsClient
        {
            RefreshResult = new WorkOsAuthenticationSuccess(session),
        };
        var tokenValidator = new StubTokenValidator
        {
            Result = new WorkOsTokenValidationResult(true, accessTokenClaims),
        };
        var service = new WorkOsAccessAuthenticationService(authenticationClient, authenticationClient, authenticationClient, tokenValidator);

        var outcome = await service.RefreshAsync(
            new AccessRefreshRequest("refresh-token-original", "org_456"),
            TestContext.Current.CancellationToken);

        var success = outcome.ShouldBeOfType<AccessAuthenticationSucceeded>().Session;
        authenticationClient.LastRefreshRequest.ShouldNotBeNull();
        authenticationClient.LastRefreshRequest.RefreshToken.ShouldBe("refresh-token-original");
        authenticationClient.LastRefreshRequest.OrganizationId.ShouldBe("org_456");
        success.AccessToken.ShouldBe("access-token-rotated");
        success.RefreshToken.ShouldBe("refresh-token-rotated");
        success.SubjectId.ShouldBe("user_123");
        success.SessionId.ShouldBe("session_123");
        success.OrganizationId.ShouldBe("org_456");
        success.Roles.ShouldBe(["admin", "owner"]);
        success.Permissions.ShouldBe(["audit:read", "users:write"]);
        success.FeatureFlags.ShouldBe(["beta-dashboard"]);
        success.Entitlements.ShouldBe(["tier:pro"]);
    }

    private static WorkOsAccessAuthenticationService CreateService(WorkOsAuthenticationResult passwordResult)
    {
        var client = new StubWorkOsClient
        {
            PasswordResult = passwordResult,
        };

        return new WorkOsAccessAuthenticationService(
            client,
            client,
            client,
            new StubTokenValidator());
    }

    private static WorkOsAuthenticationChallenge CreateChallenge(
        WorkOsChallengeKind kind,
        string? emailVerificationId = null,
        string? authenticationChallengeId = null,
        IReadOnlyCollection<WorkOsAuthenticationFactor>? factors = null,
        IReadOnlyCollection<WorkOsOrganizationChoice>? organizations = null,
        WorkOsTotpEnrollment? totpEnrollment = null)
    {
        return new WorkOsAuthenticationChallenge(
            kind,
            new WorkOsPendingAuthentication(
                "pending_123",
                "ada@example.com",
                emailVerificationId,
                authenticationChallengeId,
                factors,
                organizations,
                totpEnrollment),
            "provider_challenge",
            "Challenge required.");
    }

    private sealed class StubWorkOsClient :
        IWorkOsAuthenticationClient,
        IWorkOsMagicAuthClient,
        IWorkOsSessionClient
    {
        public WorkOsAuthenticationResult PasswordResult { get; set; } =
            new WorkOsAuthenticationFailure(new WorkOsFailure("not_configured", "Password result not configured."));

        public WorkOsAuthenticationResult RefreshResult { get; set; } =
            new WorkOsAuthenticationFailure(new WorkOsFailure("not_configured", "Refresh result not configured."));

        public WorkOsRefreshRequest? LastRefreshRequest { get; private set; }

        public Task<Uri> CreateAuthorizationUrlAsync(WorkOsAuthorizationRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(new Uri("https://auth.example.test/authorize"));

        public Task<WorkOsAuthenticationResult> ExchangeCodeAsync(WorkOsCodeExchangeRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<WorkOsAuthenticationResult> AuthenticateWithPasswordAsync(
            WorkOsPasswordAuthenticationRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(PasswordResult);

        public Task<WorkOsMagicAuthStartResult> BeginAsync(WorkOsMagicAuthStartRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<WorkOsTotpEnrollment> EnrollTotpAsync(WorkOsTotpEnrollmentRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<WorkOsAuthenticationResult> CompleteMagicAuthAsync(WorkOsMagicAuthCompletionRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<WorkOsAuthenticationResult> CompleteEmailVerificationAsync(WorkOsEmailVerificationRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<WorkOsAuthenticationResult> CompleteTotpAsync(WorkOsTotpAuthenticationRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<WorkOsAuthenticationResult> CompleteOrganizationSelectionAsync(WorkOsOrganizationSelectionRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<WorkOsAuthenticationResult> RefreshAsync(WorkOsRefreshRequest request, CancellationToken cancellationToken = default)
        {
            LastRefreshRequest = request;
            return Task.FromResult(RefreshResult);
        }

        public Task<WorkOsSessionSignOutResult> SignOutAsync(WorkOsSignOutRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(new WorkOsSessionSignOutResult(false));
    }

    private sealed class StubTokenValidator : IWorkOsTokenValidator
    {
        public WorkOsTokenValidationResult Result { get; set; } =
            new(false, FailureCode: "not_configured", FailureMessage: "Token validation result not configured.");

        public Task<WorkOsTokenValidationResult> ValidateAsync(string accessToken, CancellationToken cancellationToken = default) =>
            Task.FromResult(Result);
    }
}
