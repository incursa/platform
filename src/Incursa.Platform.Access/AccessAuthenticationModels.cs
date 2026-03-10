#pragma warning disable MA0048
namespace Incursa.Platform.Access;

public enum AccessChallengeKind
{
    EmailVerificationRequired = 0,
    MfaEnrollmentRequired = 1,
    MfaChallengeRequired = 2,
    OrganizationSelectionRequired = 3,
    IdentityLinkingRequired = 4,
    ProviderChallengeRequired = 5,
    GenericProviderChallenge = ProviderChallengeRequired,
}

public static class AccessClaimTypes
{
    public const string SessionId = "access:session_id";
    public const string OrganizationId = "access:organization_id";
    public const string Role = "access:role";
    public const string Permission = "access:permission";
    public const string FeatureFlag = "access:feature_flag";
    public const string Entitlement = "access:entitlement";
}

public sealed record AccessAuthenticationRequestMetadata
{
    public AccessAuthenticationRequestMetadata(string? ipAddress = null, string? userAgent = null)
    {
        IpAddress = string.IsNullOrWhiteSpace(ipAddress) ? null : ipAddress.Trim();
        UserAgent = string.IsNullOrWhiteSpace(userAgent) ? null : userAgent.Trim();
    }

    public string? IpAddress { get; }

    public string? UserAgent { get; }
}

public sealed record AccessRedirectAuthorizationRequest
{
    public AccessRedirectAuthorizationRequest(
        string redirectUri,
        string? provider = null,
        string? connectionId = null,
        string? organizationId = null,
        string? state = null,
        string? codeChallenge = null,
        string? codeChallengeMethod = null,
        string? loginHint = null,
        string? domainHint = null,
        string? screenHint = null,
        IReadOnlyCollection<string>? providerScopes = null,
        IReadOnlyDictionary<string, string>? additionalParameters = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(redirectUri);

        RedirectUri = redirectUri.Trim();
        Provider = string.IsNullOrWhiteSpace(provider) ? null : provider.Trim();
        ConnectionId = string.IsNullOrWhiteSpace(connectionId) ? null : connectionId.Trim();
        OrganizationId = string.IsNullOrWhiteSpace(organizationId) ? null : organizationId.Trim();
        State = string.IsNullOrWhiteSpace(state) ? null : state.Trim();
        CodeChallenge = string.IsNullOrWhiteSpace(codeChallenge) ? null : codeChallenge.Trim();
        CodeChallengeMethod = string.IsNullOrWhiteSpace(codeChallengeMethod) ? null : codeChallengeMethod.Trim();
        LoginHint = string.IsNullOrWhiteSpace(loginHint) ? null : loginHint.Trim();
        DomainHint = string.IsNullOrWhiteSpace(domainHint) ? null : domainHint.Trim();
        ScreenHint = string.IsNullOrWhiteSpace(screenHint) ? null : screenHint.Trim();
        ProviderScopes = providerScopes?.Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? Array.Empty<string>();
        AdditionalParameters = additionalParameters is null
            ? new Dictionary<string, string>(StringComparer.Ordinal)
            : new Dictionary<string, string>(
                additionalParameters
                    .Where(static pair => !string.IsNullOrWhiteSpace(pair.Key) && !string.IsNullOrWhiteSpace(pair.Value))
                    .ToDictionary(
                        static pair => pair.Key.Trim(),
                        static pair => pair.Value.Trim(),
                        StringComparer.Ordinal),
                StringComparer.Ordinal);
    }

    public string RedirectUri { get; }

    public string? Provider { get; }

    public string? ConnectionId { get; }

    public string? OrganizationId { get; }

    public string? State { get; }

    public string? CodeChallenge { get; }

    public string? CodeChallengeMethod { get; }

    public string? LoginHint { get; }

    public string? DomainHint { get; }

    public string? ScreenHint { get; }

    public IReadOnlyCollection<string> ProviderScopes { get; }

    public IReadOnlyDictionary<string, string> AdditionalParameters { get; }
}

public sealed record AccessRedirectAuthorization(Uri Url);

public sealed record AccessCodeExchangeRequest(
    string Code,
    string? RedirectUri = null,
    string? CodeVerifier = null,
    string? InvitationToken = null,
    AccessAuthenticationRequestMetadata? Metadata = null)
{
    public string Code { get; } = string.IsNullOrWhiteSpace(Code)
        ? throw new ArgumentException("Code is required.", nameof(Code))
        : Code.Trim();

    public string? RedirectUri { get; } = string.IsNullOrWhiteSpace(RedirectUri) ? null : RedirectUri.Trim();

    public string? CodeVerifier { get; } = string.IsNullOrWhiteSpace(CodeVerifier) ? null : CodeVerifier.Trim();

    public string? InvitationToken { get; } = string.IsNullOrWhiteSpace(InvitationToken) ? null : InvitationToken.Trim();
}

public sealed record AccessPasswordSignInRequest(
    string Email,
    string Password,
    AccessAuthenticationRequestMetadata? Metadata = null)
{
    public string Email { get; } = string.IsNullOrWhiteSpace(Email)
        ? throw new ArgumentException("Email is required.", nameof(Email))
        : Email.Trim();

    public string Password { get; } = string.IsNullOrWhiteSpace(Password)
        ? throw new ArgumentException("Password is required.", nameof(Password))
        : Password;
}

public sealed record AccessMagicAuthStartRequest(string Email)
{
    public string Email { get; } = string.IsNullOrWhiteSpace(Email)
        ? throw new ArgumentException("Email is required.", nameof(Email))
        : Email.Trim();

    public bool ReturnCode { get; init; }
}

public sealed record AccessTotpEnrollmentRequest(
    string Issuer,
    string User)
{
    public string Issuer { get; } = string.IsNullOrWhiteSpace(Issuer)
        ? throw new ArgumentException("Issuer is required.", nameof(Issuer))
        : Issuer.Trim();

    public string User { get; } = string.IsNullOrWhiteSpace(User)
        ? throw new ArgumentException("User is required.", nameof(User))
        : User.Trim();
}

public sealed record AccessMagicAuthCompletionRequest(
    string Code,
    AccessAuthenticationRequestMetadata? Metadata = null)
{
    public string Code { get; } = string.IsNullOrWhiteSpace(Code)
        ? throw new ArgumentException("Code is required.", nameof(Code))
        : Code.Trim();
}

public sealed record AccessEmailVerificationRequest(
    string PendingAuthenticationToken,
    string Code,
    string? EmailVerificationId = null,
    AccessAuthenticationRequestMetadata? Metadata = null)
{
    public string PendingAuthenticationToken { get; } = string.IsNullOrWhiteSpace(PendingAuthenticationToken)
        ? throw new ArgumentException("Pending authentication token is required.", nameof(PendingAuthenticationToken))
        : PendingAuthenticationToken.Trim();

    public string Code { get; } = string.IsNullOrWhiteSpace(Code)
        ? throw new ArgumentException("Code is required.", nameof(Code))
        : Code.Trim();

    public string? EmailVerificationId { get; } = string.IsNullOrWhiteSpace(EmailVerificationId) ? null : EmailVerificationId.Trim();
}

public sealed record AccessTotpCompletionRequest
{
    public AccessTotpCompletionRequest(
        string pendingAuthenticationToken,
        string? authenticationFactorId = null,
        string? authenticationChallengeId = null,
        string? code = null,
        string? enrollmentIssuer = null,
        string? enrollmentUser = null,
        AccessAuthenticationRequestMetadata? metadata = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pendingAuthenticationToken);

        PendingAuthenticationToken = pendingAuthenticationToken.Trim();
        AuthenticationFactorId = string.IsNullOrWhiteSpace(authenticationFactorId) ? null : authenticationFactorId.Trim();
        AuthenticationChallengeId = string.IsNullOrWhiteSpace(authenticationChallengeId) ? null : authenticationChallengeId.Trim();
        Code = string.IsNullOrWhiteSpace(code) ? null : code.Trim();
        EnrollmentIssuer = string.IsNullOrWhiteSpace(enrollmentIssuer) ? null : enrollmentIssuer.Trim();
        EnrollmentUser = string.IsNullOrWhiteSpace(enrollmentUser) ? null : enrollmentUser.Trim();
        Metadata = metadata;
    }

    public string PendingAuthenticationToken { get; }

    public string? AuthenticationFactorId { get; }

    public string? AuthenticationChallengeId { get; }

    public string? Code { get; }

    public string? EnrollmentIssuer { get; }

    public string? EnrollmentUser { get; }

    public AccessAuthenticationRequestMetadata? Metadata { get; }
}

public sealed record AccessOrganizationSelectionRequest(
    string PendingAuthenticationToken,
    string OrganizationId,
    AccessAuthenticationRequestMetadata? Metadata = null)
{
    public string PendingAuthenticationToken { get; } = string.IsNullOrWhiteSpace(PendingAuthenticationToken)
        ? throw new ArgumentException("Pending authentication token is required.", nameof(PendingAuthenticationToken))
        : PendingAuthenticationToken.Trim();

    public string OrganizationId { get; } = string.IsNullOrWhiteSpace(OrganizationId)
        ? throw new ArgumentException("Organization id is required.", nameof(OrganizationId))
        : OrganizationId.Trim();
}

public sealed record AccessRefreshRequest(
    string RefreshToken,
    string? OrganizationId = null,
    AccessAuthenticationRequestMetadata? Metadata = null)
{
    public string RefreshToken { get; } = string.IsNullOrWhiteSpace(RefreshToken)
        ? throw new ArgumentException("Refresh token is required.", nameof(RefreshToken))
        : RefreshToken.Trim();

    public string? OrganizationId { get; } = string.IsNullOrWhiteSpace(OrganizationId) ? null : OrganizationId.Trim();
}

public sealed record AccessSignOutRequest(string? SessionId = null, string? ReturnToUri = null)
{
    public string? SessionId { get; } = string.IsNullOrWhiteSpace(SessionId) ? null : SessionId.Trim();

    public string? ReturnToUri { get; } = string.IsNullOrWhiteSpace(ReturnToUri) ? null : ReturnToUri.Trim();
}

public sealed record AccessMagicAuthStartResult(
    string MagicAuthId,
    string Email,
    DateTimeOffset? ExpiresAtUtc = null,
    string? Code = null,
    bool EmailSent = true)
{
    public string MagicAuthId { get; } = string.IsNullOrWhiteSpace(MagicAuthId)
        ? throw new ArgumentException("Magic auth id is required.", nameof(MagicAuthId))
        : MagicAuthId.Trim();

    public string Email { get; } = string.IsNullOrWhiteSpace(Email)
        ? throw new ArgumentException("Email is required.", nameof(Email))
        : Email.Trim();

    public string? Code { get; } = string.IsNullOrWhiteSpace(Code) ? null : Code.Trim();
}

public sealed record AccessOrganizationChoice
{
    public AccessOrganizationChoice(string id, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Id = id.Trim();
        Name = name.Trim();
    }

    public string Id { get; }

    public string Name { get; }
}

public sealed record AccessAuthenticationFactor
{
    public AccessAuthenticationFactor(string id, string type)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(type);

        Id = id.Trim();
        Type = type.Trim();
    }

    public string Id { get; }

    public string Type { get; }
}

public sealed record AccessTotpEnrollment
{
    public AccessTotpEnrollment(
        string factorId,
        string? issuer = null,
        string? user = null,
        string? qrCode = null,
        string? secret = null,
        string? uri = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(factorId);

        FactorId = factorId.Trim();
        Issuer = string.IsNullOrWhiteSpace(issuer) ? null : issuer.Trim();
        User = string.IsNullOrWhiteSpace(user) ? null : user.Trim();
        QrCode = string.IsNullOrWhiteSpace(qrCode) ? null : qrCode.Trim();
        Secret = string.IsNullOrWhiteSpace(secret) ? null : secret.Trim();
        Uri = string.IsNullOrWhiteSpace(uri) ? null : uri.Trim();
    }

    public string FactorId { get; }

    public string? Issuer { get; }

    public string? User { get; }

    public string? QrCode { get; }

    public string? Secret { get; }

    public string? Uri { get; }
}

public sealed record AccessChallenge
{
    public AccessChallenge(
        AccessChallengeKind kind,
        string pendingAuthenticationToken,
        string? code = null,
        string? message = null,
        string? email = null,
        string? emailVerificationId = null,
        string? authenticationChallengeId = null,
        IReadOnlyCollection<AccessAuthenticationFactor>? factors = null,
        IReadOnlyCollection<AccessOrganizationChoice>? organizations = null,
        AccessTotpEnrollment? totpEnrollment = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pendingAuthenticationToken);

        Kind = kind;
        PendingAuthenticationToken = pendingAuthenticationToken.Trim();
        Code = string.IsNullOrWhiteSpace(code) ? null : code.Trim();
        Message = string.IsNullOrWhiteSpace(message) ? null : message.Trim();
        Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
        EmailVerificationId = string.IsNullOrWhiteSpace(emailVerificationId) ? null : emailVerificationId.Trim();
        AuthenticationChallengeId = string.IsNullOrWhiteSpace(authenticationChallengeId) ? null : authenticationChallengeId.Trim();
        Factors = factors?.ToArray() ?? Array.Empty<AccessAuthenticationFactor>();
        Organizations = organizations?.ToArray() ?? Array.Empty<AccessOrganizationChoice>();
        TotpEnrollment = totpEnrollment;
    }

    public AccessChallengeKind Kind { get; }

    public string PendingAuthenticationToken { get; }

    public string? Code { get; }

    public string? Message { get; }

    public string? Email { get; }

    public string? EmailVerificationId { get; }

    public string? AuthenticationChallengeId { get; }

    public IReadOnlyCollection<AccessAuthenticationFactor> Factors { get; }

    public IReadOnlyCollection<AccessOrganizationChoice> Organizations { get; }

    public AccessTotpEnrollment? TotpEnrollment { get; }
}

public sealed record AccessFailure
{
    public AccessFailure(string? code, string message, bool isTransient = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        Code = string.IsNullOrWhiteSpace(code) ? null : code.Trim();
        Message = message.Trim();
        IsTransient = isTransient;
    }

    public string? Code { get; }

    public string Message { get; }

    public bool IsTransient { get; }
}

public abstract record AccessAuthenticationOutcome;

public sealed record AccessAuthenticationSucceeded(AccessAuthenticatedSession Session) : AccessAuthenticationOutcome;

public sealed record AccessAuthenticationChallengeRequired(AccessChallenge Challenge) : AccessAuthenticationOutcome;

public sealed record AccessAuthenticationFailed(AccessFailure Failure) : AccessAuthenticationOutcome;

public sealed record AccessContext
{
    public AccessContext(
        string subjectId,
        string? sessionId = null,
        string? organizationId = null,
        IReadOnlyCollection<string>? roles = null,
        IReadOnlyCollection<string>? permissions = null,
        IReadOnlyCollection<string>? featureFlags = null,
        IReadOnlyCollection<string>? entitlements = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subjectId);

        SubjectId = subjectId.Trim();
        SessionId = string.IsNullOrWhiteSpace(sessionId) ? null : sessionId.Trim();
        OrganizationId = string.IsNullOrWhiteSpace(organizationId) ? null : organizationId.Trim();
        Roles = NormalizeSet(roles);
        Permissions = NormalizeSet(permissions);
        FeatureFlags = NormalizeSet(featureFlags);
        Entitlements = NormalizeSet(entitlements);
    }

    public string SubjectId { get; }

    public string? SessionId { get; }

    public string? OrganizationId { get; }

    public IReadOnlyCollection<string> Roles { get; }

    public IReadOnlyCollection<string> Permissions { get; }

    public IReadOnlyCollection<string> FeatureFlags { get; }

    public IReadOnlyCollection<string> Entitlements { get; }

    internal static IReadOnlyCollection<string> NormalizeSet(IReadOnlyCollection<string>? values) =>
        values?.Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? Array.Empty<string>();
}

public sealed record AccessAuthenticatedSession
{
    public AccessAuthenticatedSession(
        string subjectId,
        string accessToken,
        string? refreshToken = null,
        string? sessionId = null,
        string? organizationId = null,
        IReadOnlyCollection<string>? roles = null,
        IReadOnlyCollection<string>? permissions = null,
        IReadOnlyCollection<string>? featureFlags = null,
        IReadOnlyCollection<string>? entitlements = null,
        string? email = null,
        string? displayName = null,
        bool? emailVerified = null,
        string tokenType = "Bearer",
        DateTimeOffset? accessTokenExpiresAtUtc = null,
        DateTimeOffset? refreshTokenExpiresAtUtc = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subjectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenType);

        SubjectId = subjectId.Trim();
        AccessToken = accessToken.Trim();
        RefreshToken = string.IsNullOrWhiteSpace(refreshToken) ? null : refreshToken.Trim();
        SessionId = string.IsNullOrWhiteSpace(sessionId) ? null : sessionId.Trim();
        OrganizationId = string.IsNullOrWhiteSpace(organizationId) ? null : organizationId.Trim();
        Roles = AccessContext.NormalizeSet(roles);
        Permissions = AccessContext.NormalizeSet(permissions);
        FeatureFlags = AccessContext.NormalizeSet(featureFlags);
        Entitlements = AccessContext.NormalizeSet(entitlements);
        Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim();
        EmailVerified = emailVerified;
        TokenType = tokenType.Trim();
        AccessTokenExpiresAtUtc = accessTokenExpiresAtUtc;
        RefreshTokenExpiresAtUtc = refreshTokenExpiresAtUtc;
        AccessContext = new AccessContext(
            SubjectId,
            SessionId,
            OrganizationId,
            Roles,
            Permissions,
            FeatureFlags,
            Entitlements);
    }

    public string SubjectId { get; }

    public string AccessToken { get; }

    public string? RefreshToken { get; }

    public string? SessionId { get; }

    public string? OrganizationId { get; }

    public IReadOnlyCollection<string> Roles { get; }

    public IReadOnlyCollection<string> Permissions { get; }

    public IReadOnlyCollection<string> FeatureFlags { get; }

    public IReadOnlyCollection<string> Entitlements { get; }

    public string? Email { get; }

    public string? DisplayName { get; }

    public bool? EmailVerified { get; }

    public string TokenType { get; }

    public DateTimeOffset? AccessTokenExpiresAtUtc { get; }

    public DateTimeOffset? RefreshTokenExpiresAtUtc { get; }

    public AccessContext AccessContext { get; }
}

public sealed record AccessSignOutResult(bool ProviderSessionRevoked, Uri? LogoutUrl = null);
#pragma warning restore MA0048
