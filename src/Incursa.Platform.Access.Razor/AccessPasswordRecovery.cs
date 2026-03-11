#pragma warning disable MA0048
namespace Incursa.Platform.Access.Razor;

public interface IAccessPasswordRecoveryService
{
    Task<AccessPasswordRecoveryResult> RequestResetAsync(
        AccessPasswordResetRequest request,
        CancellationToken cancellationToken = default);

    Task<AccessPasswordRecoveryResult> ResetPasswordAsync(
        AccessPasswordResetCompletionRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record AccessPasswordResetRequest(
    string Email,
    string? ReturnUrl = null,
    AccessAuthenticationRequestMetadata? Metadata = null)
{
    public string Email { get; } = string.IsNullOrWhiteSpace(Email)
        ? throw new ArgumentException("Email is required.", nameof(Email))
        : Email.Trim();

    public string? ReturnUrl { get; } = string.IsNullOrWhiteSpace(ReturnUrl) ? null : ReturnUrl.Trim();
}

public sealed record AccessPasswordResetCompletionRequest(
    string Token,
    string NewPassword,
    AccessAuthenticationRequestMetadata? Metadata = null)
{
    public string Token { get; } = string.IsNullOrWhiteSpace(Token)
        ? throw new ArgumentException("Reset token is required.", nameof(Token))
        : Token.Trim();

    public string NewPassword { get; } = string.IsNullOrWhiteSpace(NewPassword)
        ? throw new ArgumentException("A new password is required.", nameof(NewPassword))
        : NewPassword;
}

public sealed record AccessPasswordRecoveryResult(bool Accepted, string? Message = null);
#pragma warning restore MA0048
