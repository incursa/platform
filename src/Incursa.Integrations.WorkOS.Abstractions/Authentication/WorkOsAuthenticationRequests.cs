#pragma warning disable MA0048
namespace Incursa.Integrations.WorkOS.Abstractions.Authentication;

public sealed record WorkOsRequestMetadata
{
    public WorkOsRequestMetadata(string? ipAddress = null, string? userAgent = null)
    {
        IpAddress = string.IsNullOrWhiteSpace(ipAddress) ? null : ipAddress.Trim();
        UserAgent = string.IsNullOrWhiteSpace(userAgent) ? null : userAgent.Trim();
    }

    public string? IpAddress { get; }

    public string? UserAgent { get; }
}

public sealed record WorkOsAuthorizationRequest(
    string RedirectUri,
    string? Provider = null,
    string? ConnectionId = null,
    string? OrganizationId = null,
    string? State = null,
    string? CodeChallenge = null,
    string? CodeChallengeMethod = null,
    string? LoginHint = null,
    string? DomainHint = null,
    string? ScreenHint = null,
    IReadOnlyCollection<string>? ProviderScopes = null,
    IReadOnlyDictionary<string, string>? AdditionalParameters = null);

public sealed record WorkOsCodeExchangeRequest(
    string Code,
    string RedirectUri,
    string? CodeVerifier = null,
    string? InvitationToken = null,
    WorkOsRequestMetadata? Metadata = null)
{
    public string Code { get; } = string.IsNullOrWhiteSpace(Code)
        ? throw new ArgumentException("Code is required.", nameof(Code))
        : Code.Trim();

    public string RedirectUri { get; } = string.IsNullOrWhiteSpace(RedirectUri)
        ? throw new ArgumentException("Redirect uri is required.", nameof(RedirectUri))
        : RedirectUri.Trim();

    public string? CodeVerifier { get; } = string.IsNullOrWhiteSpace(CodeVerifier) ? null : CodeVerifier.Trim();

    public string? InvitationToken { get; } = string.IsNullOrWhiteSpace(InvitationToken) ? null : InvitationToken.Trim();
}

public sealed record WorkOsPasswordAuthenticationRequest(
    string Email,
    string Password,
    WorkOsRequestMetadata? Metadata = null)
{
    public string Email { get; } = string.IsNullOrWhiteSpace(Email)
        ? throw new ArgumentException("Email is required.", nameof(Email))
        : Email.Trim();

    public string Password { get; } = string.IsNullOrWhiteSpace(Password)
        ? throw new ArgumentException("Password is required.", nameof(Password))
        : Password;
}

public sealed record WorkOsMagicAuthStartRequest(string Email, bool ReturnCode = false)
{
    public string Email { get; } = string.IsNullOrWhiteSpace(Email)
        ? throw new ArgumentException("Email is required.", nameof(Email))
        : Email.Trim();
}

public sealed record WorkOsMagicAuthCompletionRequest(
    string Code,
    WorkOsRequestMetadata? Metadata = null)
{
    public string Code { get; } = string.IsNullOrWhiteSpace(Code)
        ? throw new ArgumentException("Code is required.", nameof(Code))
        : Code.Trim();
}

public sealed record WorkOsEmailVerificationRequest(
    string PendingAuthenticationToken,
    string Code,
    string? EmailVerificationId = null,
    WorkOsRequestMetadata? Metadata = null)
{
    public string PendingAuthenticationToken { get; } = string.IsNullOrWhiteSpace(PendingAuthenticationToken)
        ? throw new ArgumentException("Pending authentication token is required.", nameof(PendingAuthenticationToken))
        : PendingAuthenticationToken.Trim();

    public string Code { get; } = string.IsNullOrWhiteSpace(Code)
        ? throw new ArgumentException("Code is required.", nameof(Code))
        : Code.Trim();

    public string? EmailVerificationId { get; } = string.IsNullOrWhiteSpace(EmailVerificationId) ? null : EmailVerificationId.Trim();
}

public sealed record WorkOsTotpAuthenticationRequest
{
    public WorkOsTotpAuthenticationRequest(
        string pendingAuthenticationToken,
        string? authenticationFactorId = null,
        string? authenticationChallengeId = null,
        string? code = null,
        string? enrollmentIssuer = null,
        string? enrollmentUser = null,
        WorkOsRequestMetadata? metadata = null)
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

    public WorkOsRequestMetadata? Metadata { get; }
}

public sealed record WorkOsOrganizationSelectionRequest(
    string PendingAuthenticationToken,
    string OrganizationId,
    WorkOsRequestMetadata? Metadata = null)
{
    public string PendingAuthenticationToken { get; } = string.IsNullOrWhiteSpace(PendingAuthenticationToken)
        ? throw new ArgumentException("Pending authentication token is required.", nameof(PendingAuthenticationToken))
        : PendingAuthenticationToken.Trim();

    public string OrganizationId { get; } = string.IsNullOrWhiteSpace(OrganizationId)
        ? throw new ArgumentException("Organization id is required.", nameof(OrganizationId))
        : OrganizationId.Trim();
}

public sealed record WorkOsRefreshRequest(
    string RefreshToken,
    string? OrganizationId = null,
    WorkOsRequestMetadata? Metadata = null)
{
    public string RefreshToken { get; } = string.IsNullOrWhiteSpace(RefreshToken)
        ? throw new ArgumentException("Refresh token is required.", nameof(RefreshToken))
        : RefreshToken.Trim();

    public string? OrganizationId { get; } = string.IsNullOrWhiteSpace(OrganizationId) ? null : OrganizationId.Trim();
}

public sealed record WorkOsTotpEnrollmentRequest(string User, string? Issuer = null);

public sealed record WorkOsSignOutRequest(
    string? SessionId = null,
    string? ReturnToUri = null)
{
    public string? SessionId { get; } = string.IsNullOrWhiteSpace(SessionId) ? null : SessionId.Trim();

    public string? ReturnToUri { get; } = string.IsNullOrWhiteSpace(ReturnToUri) ? null : ReturnToUri.Trim();
}
#pragma warning restore MA0048
