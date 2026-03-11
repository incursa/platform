#pragma warning disable MA0048
namespace Incursa.Integrations.WorkOS.Access;

using System.Text.Json.Serialization;

internal sealed class AuthenticationSuccessDto
{
    [JsonPropertyName("user")]
    public UserDto? User { get; set; }

    [JsonPropertyName("organization_id")]
    public string? OrganizationId { get; set; }

    [JsonPropertyName("access_token")]
    public string? AccessToken { get; set; }

    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }

    [JsonPropertyName("sealed_session")]
    public string? SealedSession { get; set; }

    [JsonPropertyName("authentication_method")]
    public AuthenticationMethodDto? AuthenticationMethod { get; set; }
}

internal sealed class AuthenticationMethodDto
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }
}

internal sealed class AuthenticationErrorDto
{
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("error_description")]
    public string? ErrorDescription { get; set; }

    [JsonPropertyName("pending_authentication_token")]
    public string? PendingAuthenticationToken { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("email_verification_id")]
    public string? EmailVerificationId { get; set; }

    [JsonPropertyName("authentication_challenge_id")]
    public string? AuthenticationChallengeId { get; set; }

    [JsonPropertyName("user")]
    public UserDto? User { get; set; }

    [JsonPropertyName("authentication_factors")]
    public FactorDto[]? AuthenticationFactors { get; set; }

    [JsonPropertyName("organizations")]
    public OrganizationDto[]? Organizations { get; set; }
}

internal sealed class UserDto
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("first_name")]
    public string? FirstName { get; set; }

    [JsonPropertyName("last_name")]
    public string? LastName { get; set; }

    [JsonPropertyName("email_verified")]
    public bool? EmailVerified { get; set; }
}

internal sealed class OrganizationDto
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("external_id")]
    public string? ExternalId { get; set; }
}

internal sealed class MagicAuthCreateDto
{
    [JsonPropertyName("email")]
    public string? Email { get; set; }
}

internal sealed class MagicAuthDto
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }

    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("expires_at")]
    public string? ExpiresAt { get; set; }
}

internal sealed class EmailVerificationDto
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("expires_at")]
    public string? ExpiresAt { get; set; }
}

internal sealed class EnrollTotpFactorDto
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("totp_issuer")]
    public string? Issuer { get; set; }

    [JsonPropertyName("totp_user")]
    public string? User { get; set; }
}

internal sealed class FactorDto
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("expires_at")]
    public string? ExpiresAt { get; set; }

    [JsonPropertyName("totp")]
    public TotpFactorDto? Totp { get; set; }
}

internal sealed class TotpFactorDto
{
    [JsonPropertyName("issuer")]
    public string? Issuer { get; set; }

    [JsonPropertyName("user")]
    public string? User { get; set; }

    [JsonPropertyName("qr_code")]
    public string? QrCode { get; set; }

    [JsonPropertyName("secret")]
    public string? Secret { get; set; }

    [JsonPropertyName("uri")]
    public string? Uri { get; set; }

    [JsonPropertyName("code")]
    public string? Code { get; set; }
}

internal sealed class ChallengeDto
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("expires_at")]
    public string? ExpiresAt { get; set; }

    [JsonPropertyName("code")]
    public string? Code { get; set; }
}

internal sealed class PasswordResetDto
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("password_reset_token")]
    public string? PasswordResetToken { get; set; }

    [JsonPropertyName("password_reset_url")]
    public string? PasswordResetUrl { get; set; }

    [JsonPropertyName("expires_at")]
    public string? ExpiresAt { get; set; }
}

internal sealed class PasswordResetCreateDto
{
    [JsonPropertyName("email")]
    public string? Email { get; set; }
}

internal sealed class PasswordResetConfirmDto
{
    [JsonPropertyName("token")]
    public string? Token { get; set; }

    [JsonPropertyName("new_password")]
    public string? NewPassword { get; set; }
}
#pragma warning restore MA0048
