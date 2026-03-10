#pragma warning disable MA0048
namespace Incursa.Integrations.WorkOS.Abstractions.Authentication;

using System.Security.Claims;

public enum WorkOsChallengeKind
{
    EmailVerificationRequired = 0,
    MfaEnrollmentRequired = 1,
    MfaChallengeRequired = 2,
    OrganizationSelectionRequired = 3,
    IdentityLinkingRequired = 4,
    ProviderChallengeRequired = 5,
    GenericProviderChallenge = ProviderChallengeRequired,
}

public sealed record WorkOsOrganizationChoice
{
    public WorkOsOrganizationChoice(string id, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Id = id.Trim();
        Name = name.Trim();
    }

    public string Id { get; }

    public string Name { get; }
}

public sealed record WorkOsAuthenticationFactor
{
    public WorkOsAuthenticationFactor(string id, string type)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(type);

        Id = id.Trim();
        Type = type.Trim();
    }

    public string Id { get; }

    public string Type { get; }
}

public sealed record WorkOsTotpEnrollment
{
    public WorkOsTotpEnrollment(
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

public sealed record WorkOsPendingAuthentication
{
    public WorkOsPendingAuthentication(
        string pendingAuthenticationToken,
        string? email = null,
        string? emailVerificationId = null,
        string? authenticationChallengeId = null,
        IReadOnlyCollection<WorkOsAuthenticationFactor>? factors = null,
        IReadOnlyCollection<WorkOsOrganizationChoice>? organizations = null,
        WorkOsTotpEnrollment? totpEnrollment = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pendingAuthenticationToken);

        PendingAuthenticationToken = pendingAuthenticationToken.Trim();
        Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
        EmailVerificationId = string.IsNullOrWhiteSpace(emailVerificationId) ? null : emailVerificationId.Trim();
        AuthenticationChallengeId = string.IsNullOrWhiteSpace(authenticationChallengeId) ? null : authenticationChallengeId.Trim();
        Factors = factors?.ToArray() ?? Array.Empty<WorkOsAuthenticationFactor>();
        Organizations = organizations?.ToArray() ?? Array.Empty<WorkOsOrganizationChoice>();
        TotpEnrollment = totpEnrollment;
    }

    public string PendingAuthenticationToken { get; }

    public string? Email { get; }

    public string? EmailVerificationId { get; }

    public string? AuthenticationChallengeId { get; }

    public IReadOnlyCollection<WorkOsAuthenticationFactor> Factors { get; }

    public IReadOnlyCollection<WorkOsOrganizationChoice> Organizations { get; }

    public WorkOsTotpEnrollment? TotpEnrollment { get; }
}

public sealed record WorkOsFailure
{
    public WorkOsFailure(string? code, string message, bool isTransient = false, string? rawError = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        Code = string.IsNullOrWhiteSpace(code) ? null : code.Trim();
        Message = message.Trim();
        IsTransient = isTransient;
        RawError = string.IsNullOrWhiteSpace(rawError) ? null : rawError.Trim();
    }

    public string? Code { get; }

    public string Message { get; }

    public bool IsTransient { get; }

    public string? RawError { get; }
}

public sealed record WorkOsTokenClaims
{
    public WorkOsTokenClaims(
        string subjectId,
        string? sessionId = null,
        string? organizationId = null,
        IReadOnlyCollection<string>? roles = null,
        IReadOnlyCollection<string>? permissions = null,
        IReadOnlyCollection<string>? featureFlags = null,
        IReadOnlyCollection<string>? entitlements = null,
        DateTimeOffset? expiresAtUtc = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subjectId);

        SubjectId = subjectId.Trim();
        SessionId = string.IsNullOrWhiteSpace(sessionId) ? null : sessionId.Trim();
        OrganizationId = string.IsNullOrWhiteSpace(organizationId) ? null : organizationId.Trim();
        Roles = Normalize(roles);
        Permissions = Normalize(permissions);
        FeatureFlags = Normalize(featureFlags);
        Entitlements = Normalize(entitlements);
        ExpiresAtUtc = expiresAtUtc;
    }

    public string SubjectId { get; }

    public string? SessionId { get; }

    public string? OrganizationId { get; }

    public IReadOnlyCollection<string> Roles { get; }

    public IReadOnlyCollection<string> Permissions { get; }

    public IReadOnlyCollection<string> FeatureFlags { get; }

    public IReadOnlyCollection<string> Entitlements { get; }

    public DateTimeOffset? ExpiresAtUtc { get; }

    internal static IReadOnlyCollection<string> Normalize(IReadOnlyCollection<string>? values) =>
        values?.Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? Array.Empty<string>();
}

public sealed record WorkOsAuthenticatedSession
{
    public WorkOsAuthenticatedSession(
        string subjectId,
        string accessToken,
        WorkOsTokenClaims claims,
        string? refreshToken = null,
        string? email = null,
        string? displayName = null,
        bool? emailVerified = null,
        string tokenType = "Bearer",
        DateTimeOffset? accessTokenExpiresAtUtc = null,
        DateTimeOffset? refreshTokenExpiresAtUtc = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subjectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);
        ArgumentNullException.ThrowIfNull(claims);
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenType);

        SubjectId = subjectId.Trim();
        AccessToken = accessToken.Trim();
        Claims = claims;
        RefreshToken = string.IsNullOrWhiteSpace(refreshToken) ? null : refreshToken.Trim();
        Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim();
        EmailVerified = emailVerified;
        TokenType = tokenType.Trim();
        AccessTokenExpiresAtUtc = accessTokenExpiresAtUtc;
        RefreshTokenExpiresAtUtc = refreshTokenExpiresAtUtc;
    }

    public string SubjectId { get; }

    public string AccessToken { get; }

    public WorkOsTokenClaims Claims { get; }

    public string? RefreshToken { get; }

    public string? Email { get; }

    public string? DisplayName { get; }

    public bool? EmailVerified { get; }

    public string TokenType { get; }

    public DateTimeOffset? AccessTokenExpiresAtUtc { get; }

    public DateTimeOffset? RefreshTokenExpiresAtUtc { get; }
}

public sealed record WorkOsMagicAuthStartResult(
    string Id,
    string Email,
    DateTimeOffset? ExpiresAtUtc = null,
    string? Code = null)
{
    public string Id { get; } = string.IsNullOrWhiteSpace(Id)
        ? throw new ArgumentException("Id is required.", nameof(Id))
        : Id.Trim();

    public string Email { get; } = string.IsNullOrWhiteSpace(Email)
        ? throw new ArgumentException("Email is required.", nameof(Email))
        : Email.Trim();

    public string? Code { get; } = string.IsNullOrWhiteSpace(Code) ? null : Code.Trim();
}

public sealed record WorkOsMagicAuth(
    string Id,
    string Email,
    string? UserId = null,
    string? Code = null,
    DateTimeOffset? ExpiresAtUtc = null);

public sealed record WorkOsEmailVerification(
    string Id,
    string? Email = null,
    string? Code = null,
    DateTimeOffset? ExpiresAtUtc = null);

public sealed record WorkOsTotpChallenge(
    string ChallengeId,
    string? Code = null,
    DateTimeOffset? ExpiresAtUtc = null);

public sealed record WorkOsSessionSignOutResult(bool SessionRevoked, Uri? LogoutUrl = null);

public sealed record WorkOsTokenValidationResult(
    bool IsValid,
    WorkOsTokenClaims? Claims = null,
    ClaimsPrincipal? Principal = null,
    string? FailureCode = null,
    string? FailureMessage = null);

public abstract record WorkOsAuthenticationResult;

public sealed record WorkOsAuthenticationSuccess(WorkOsAuthenticatedSession Session) : WorkOsAuthenticationResult;

public sealed record WorkOsAuthenticationChallenge(
    WorkOsChallengeKind Kind,
    WorkOsPendingAuthentication PendingAuthentication,
    string? Code = null,
    string? Message = null) : WorkOsAuthenticationResult;

public sealed record WorkOsAuthenticationFailure : WorkOsAuthenticationResult
{
    public WorkOsAuthenticationFailure(WorkOsFailure failure)
    {
        Failure = failure ?? throw new ArgumentNullException(nameof(failure));
        Code = failure.Code;
        Message = failure.Message;
        IsRetryable = failure.IsTransient;
    }

    public WorkOsFailure Failure { get; }

    public string? Code { get; }

    public string? Message { get; }

    public bool IsRetryable { get; }
}
#pragma warning restore MA0048
