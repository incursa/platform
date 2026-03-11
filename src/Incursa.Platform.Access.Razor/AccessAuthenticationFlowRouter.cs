#pragma warning disable MA0048
using Incursa.Platform.Access.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Incursa.Platform.Access.Razor;

public sealed class AccessAuthenticationFlowRouter
{
    private readonly IAccessAuthenticationTicketService ticketService;
    private readonly AccessAuthenticationStateStore stateStore;
    private readonly AccessAuthenticationUiOptions options;
    private readonly ILogger<AccessAuthenticationFlowRouter> logger;

    public AccessAuthenticationFlowRouter(
        IAccessAuthenticationTicketService ticketService,
        AccessAuthenticationStateStore stateStore,
        IOptions<AccessAuthenticationUiOptions> options,
        ILogger<AccessAuthenticationFlowRouter> logger)
    {
        this.ticketService = ticketService ?? throw new ArgumentNullException(nameof(ticketService));
        this.stateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));
        ArgumentNullException.ThrowIfNull(options);
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.options = options.Value;
    }

    public async Task<AccessAuthenticationFlowResult> HandleAsync(
        HttpContext httpContext,
        AccessAuthenticationOutcome outcome,
        string? returnUrl,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(outcome);

        returnUrl = AccessAuthenticationRequestHelpers.NormalizeReturnUrl(returnUrl) ?? options.DefaultReturnUrl;

        switch (outcome)
        {
            case AccessAuthenticationSucceeded success:
                stateStore.ClearPendingChallenge(httpContext);
                await ticketService.SignInAsync(
                    httpContext,
                    success.Session,
                    new AuthenticationProperties
                    {
                        AllowRefresh = true,
                        IsPersistent = true,
                    },
                    cancellationToken).ConfigureAwait(false);
                return new AccessAuthenticationFlowResult(RedirectUri: returnUrl);

            case AccessAuthenticationChallengeRequired challengeRequired:
                stateStore.SavePendingChallenge(
                    httpContext,
                    AccessPendingAuthenticationState.FromChallenge(challengeRequired.Challenge, returnUrl));
                return new AccessAuthenticationFlowResult(RedirectUri: ResolveChallengePath(challengeRequired.Challenge.Kind));

            case AccessAuthenticationFailed failed:
                logger.LogWarning(
                    "Authentication failed. Code={Code} Message={Message} Path={Path}",
                    failed.Failure.Code,
                    failed.Failure.Message,
                    httpContext.Request.Path.Value);
                return new AccessAuthenticationFlowResult(ErrorMessage: ResolveUserMessage(failed.Failure));

            default:
                return new AccessAuthenticationFlowResult(ErrorMessage: "Authentication could not be completed.");
        }
    }

    private string ResolveChallengePath(AccessChallengeKind kind)
    {
        return kind switch
        {
            AccessChallengeKind.EmailVerificationRequired => options.Routes.VerifyEmailPath,
            AccessChallengeKind.MfaEnrollmentRequired => options.Routes.MfaSetupPath,
            AccessChallengeKind.MfaChallengeRequired => options.Routes.MfaVerifyPath,
            AccessChallengeKind.OrganizationSelectionRequired => options.Routes.OrganizationSelectionPath,
            _ => $"{options.Routes.ErrorPath}?code=unsupported-challenge",
        };
    }

    private static string ResolveUserMessage(AccessFailure failure)
    {
        ArgumentNullException.ThrowIfNull(failure);

        var code = failure.Code ?? string.Empty;
        var message = failure.Message ?? string.Empty;
        string summary;

        if (Contains(code, "invalid") || Contains(code, "credentials") || Contains(message, "password"))
        {
            summary = "The sign-in details were rejected.";
            return AppendFailureDetails(summary, code, message);
        }

        if (Contains(code, "expired") || Contains(message, "expired"))
        {
            summary = "That step has expired. Start again and request a fresh code.";
            return AppendFailureDetails(summary, code, message);
        }

        if (Contains(code, "code") || Contains(message, "code"))
        {
            summary = "The verification code was not accepted. Check it and try again.";
            return AppendFailureDetails(summary, code, message);
        }

        summary = "Authentication could not be completed. Try again.";
        return AppendFailureDetails(summary, code, message);
    }

    private static bool Contains(string value, string fragment) =>
        value.Contains(fragment, StringComparison.OrdinalIgnoreCase);

    private static string AppendFailureDetails(string summary, string code, string message)
    {
        var details = new List<string>(2);
        if (!string.IsNullOrWhiteSpace(code))
        {
            details.Add($"code: {code.Trim()}");
        }

        if (!string.IsNullOrWhiteSpace(message))
        {
            details.Add($"message: {message.Trim()}");
        }

        return details.Count == 0
            ? summary
            : $"{summary} ({string.Join("; ", details)})";
    }
}

public sealed record AccessAuthenticationFlowResult(string? RedirectUri = null, string? ErrorMessage = null);
#pragma warning restore MA0048
