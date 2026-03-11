#pragma warning disable MA0048
namespace Incursa.Platform.Access.Razor;

using Incursa.Platform.Access.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

public static class UnavailableAccessAuthenticationUiServiceCollectionExtensions
{
    public static IServiceCollection AddUnavailableAccessAuthenticationUi(
        this IServiceCollection services,
        string? message = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<AccessAuthenticationUiUnavailableOptions>();
        if (!string.IsNullOrWhiteSpace(message))
        {
            services.Configure<AccessAuthenticationUiUnavailableOptions>(options => options.Message = message.Trim());
        }

        services.TryAddScoped<IAccessAuthenticationService, UnavailableAccessAuthenticationService>();
        services.TryAddScoped<IAccessAuthenticationTicketService, UnavailableAccessAuthenticationTicketService>();
        services.TryAddScoped<IAccessPasswordRecoveryService, UnavailableAccessPasswordRecoveryService>();
        return services;
    }
}

public sealed class AccessAuthenticationUiUnavailableOptions
{
    public string Message { get; set; } = "Authentication is not configured.";
}

public sealed class UnavailableAccessAuthenticationService : IAccessAuthenticationService
{
    private readonly AccessAuthenticationFailed unavailableOutcome;
    private readonly string message;

    public UnavailableAccessAuthenticationService(IOptions<AccessAuthenticationUiUnavailableOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        message = string.IsNullOrWhiteSpace(options.Value.Message)
            ? "Authentication is not configured."
            : options.Value.Message.Trim();
        unavailableOutcome = new AccessAuthenticationFailed(new AccessFailure("auth_not_configured", message));
    }

    public Task<AccessRedirectAuthorization> CreateAuthorizationUrlAsync(
        AccessRedirectAuthorizationRequest request,
        CancellationToken cancellationToken = default) =>
        Task.FromException<AccessRedirectAuthorization>(new InvalidOperationException(message));

    public Task<AccessAuthenticationOutcome> ExchangeCodeAsync(
        AccessCodeExchangeRequest request,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<AccessAuthenticationOutcome>(unavailableOutcome);

    public Task<AccessAuthenticationOutcome> SignInWithPasswordAsync(
        AccessPasswordSignInRequest request,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<AccessAuthenticationOutcome>(unavailableOutcome);

    public Task<AccessMagicAuthStartResult> BeginMagicAuthAsync(
        AccessMagicAuthStartRequest request,
        CancellationToken cancellationToken = default) =>
        Task.FromException<AccessMagicAuthStartResult>(new InvalidOperationException(message));

    public Task<AccessTotpEnrollment> EnrollTotpAsync(
        AccessTotpEnrollmentRequest request,
        CancellationToken cancellationToken = default) =>
        Task.FromException<AccessTotpEnrollment>(new InvalidOperationException(message));

    public Task<AccessAuthenticationOutcome> CompleteMagicAuthAsync(
        AccessMagicAuthCompletionRequest request,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<AccessAuthenticationOutcome>(unavailableOutcome);

    public Task<AccessAuthenticationOutcome> CompleteEmailVerificationAsync(
        AccessEmailVerificationRequest request,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<AccessAuthenticationOutcome>(unavailableOutcome);

    public Task<AccessAuthenticationOutcome> CompleteTotpAsync(
        AccessTotpCompletionRequest request,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<AccessAuthenticationOutcome>(unavailableOutcome);

    public Task<AccessAuthenticationOutcome> CompleteOrganizationSelectionAsync(
        AccessOrganizationSelectionRequest request,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<AccessAuthenticationOutcome>(unavailableOutcome);

    public Task<AccessAuthenticationOutcome> RefreshAsync(
        AccessRefreshRequest request,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<AccessAuthenticationOutcome>(unavailableOutcome);

    public Task<AccessSignOutResult> SignOutAsync(
        AccessSignOutRequest request,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new AccessSignOutResult(false));
}

public sealed class UnavailableAccessAuthenticationTicketService : IAccessAuthenticationTicketService
{
    public Task SignInAsync(
        HttpContext httpContext,
        AccessAuthenticatedSession session,
        AuthenticationProperties? properties = null,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task<AccessSignOutResult> SignOutAsync(
        HttpContext httpContext,
        AccessSignOutRequest? request = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new AccessSignOutResult(false));
}

public sealed class UnavailableAccessPasswordRecoveryService : IAccessPasswordRecoveryService
{
    private readonly string message;

    public UnavailableAccessPasswordRecoveryService(IOptions<AccessAuthenticationUiUnavailableOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        message = string.IsNullOrWhiteSpace(options.Value.Message)
            ? "Authentication is not configured."
            : options.Value.Message.Trim();
    }

    public Task<AccessPasswordRecoveryResult> RequestResetAsync(
        AccessPasswordResetRequest request,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new AccessPasswordRecoveryResult(false, message));

    public Task<AccessPasswordRecoveryResult> ResetPasswordAsync(
        AccessPasswordResetCompletionRequest request,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new AccessPasswordRecoveryResult(false, message));
}
#pragma warning restore MA0048
