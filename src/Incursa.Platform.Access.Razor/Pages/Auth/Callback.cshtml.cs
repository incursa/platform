namespace Incursa.Platform.Access.Razor.Pages.Auth;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

[AllowAnonymous]
public sealed class CallbackModel : PageModel
{
    private readonly IAccessAuthenticationService authenticationService;
    private readonly AccessAuthenticationFlowRouter flowRouter;
    private readonly AccessAuthenticationStateStore stateStore;
    private readonly AccessAuthenticationUiOptions uiOptions;

    public CallbackModel(
        IAccessAuthenticationService authenticationService,
        AccessAuthenticationFlowRouter flowRouter,
        AccessAuthenticationStateStore stateStore,
        IOptions<AccessAuthenticationUiOptions> uiOptions)
    {
        this.authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
        this.flowRouter = flowRouter ?? throw new ArgumentNullException(nameof(flowRouter));
        this.stateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));
        ArgumentNullException.ThrowIfNull(uiOptions);
        this.uiOptions = uiOptions.Value;
    }

    public async Task<IActionResult> OnGetAsync(
        [FromQuery] string? code,
        [FromQuery] string? state,
        [FromQuery(Name = "error")] string? providerError,
        [FromQuery(Name = "error_description")] string? providerErrorDescription,
        CancellationToken cancellationToken)
    {
        if (!uiOptions.IsConfigured)
        {
            return LocalRedirect(uiOptions.Routes.SignInPath);
        }

        if (!string.IsNullOrWhiteSpace(providerError))
        {
            return LocalRedirect(QueryHelpers.AddQueryString(
                uiOptions.Routes.ErrorPath,
                new Dictionary<string, string?>(StringComparer.Ordinal)
                {
                    ["code"] = providerError,
                    ["description"] = providerErrorDescription,
                }));
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            return LocalRedirect(QueryHelpers.AddQueryString(uiOptions.Routes.ErrorPath, "code", "missing-code"));
        }

        var redirectState = stateStore.ConsumeRedirectState(HttpContext, state);
        if (redirectState is null)
        {
            return LocalRedirect(QueryHelpers.AddQueryString(uiOptions.Routes.ErrorPath, "code", "invalid-state"));
        }

        var callbackUrl = AccessAuthenticationRequestHelpers.BuildAppAbsoluteUrl(
            Request,
            uiOptions.PublicBaseUrl,
            uiOptions.Routes.CallbackPath);
        var outcome = await authenticationService
            .ExchangeCodeAsync(
                new AccessCodeExchangeRequest(
                    code,
                    callbackUrl,
                    Metadata: AccessAuthenticationRequestHelpers.BuildMetadata(HttpContext)),
                cancellationToken)
            .ConfigureAwait(false);

        var handled = await flowRouter.HandleAsync(HttpContext, outcome, redirectState.ReturnUrl, cancellationToken).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(handled.RedirectUri))
        {
            return LocalRedirect(handled.RedirectUri);
        }

        return LocalRedirect(QueryHelpers.AddQueryString(
            uiOptions.Routes.ErrorPath,
            new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["code"] = "callback-failed",
                ["description"] = handled.ErrorMessage,
            }));
    }
}
