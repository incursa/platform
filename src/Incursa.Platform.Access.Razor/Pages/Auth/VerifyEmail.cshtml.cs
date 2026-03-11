namespace Incursa.Platform.Access.Razor.Pages.Auth;

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

[AllowAnonymous]
public sealed class VerifyEmailModel : PageModel
{
    private readonly IAccessAuthenticationService authenticationService;
    private readonly AccessAuthenticationFlowRouter flowRouter;
    private readonly AccessAuthenticationStateStore stateStore;
    private readonly AccessAuthenticationUiOptions uiOptions;

    public VerifyEmailModel(
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

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string Email { get; private set; } = "your email";

    public string ReturnUrl { get; private set; } = "/";

    public string SignInPath => AccessAuthenticationUiEndpointRouteBuilderExtensions.AppendReturnUrl(uiOptions.Routes.SignInPath, ReturnUrl);

    public IActionResult OnGet()
    {
        var state = RequireState();
        return state is null ? LocalRedirect(uiOptions.Routes.SessionExpiredPath) : Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var state = RequireState();
        if (state is null)
        {
            return LocalRedirect(uiOptions.Routes.SessionExpiredPath);
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var outcome = await authenticationService
            .CompleteEmailVerificationAsync(
                new AccessEmailVerificationRequest(
                    state.PendingAuthenticationToken,
                    Input.Code,
                    state.EmailVerificationId,
                    AccessAuthenticationRequestHelpers.BuildMetadata(HttpContext)),
                cancellationToken)
            .ConfigureAwait(false);

        var handled = await flowRouter.HandleAsync(HttpContext, outcome, state.ReturnUrl, cancellationToken).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(handled.RedirectUri))
        {
            return LocalRedirect(handled.RedirectUri);
        }

        ModelState.AddModelError(string.Empty, handled.ErrorMessage ?? "The verification code was not accepted.");
        return Page();
    }

    private AccessPendingAuthenticationState? RequireState()
    {
        var state = stateStore.GetPendingChallenge(HttpContext);
        if (state is null || state.Kind != AccessChallengeKind.EmailVerificationRequired)
        {
            return null;
        }

        Email = state.Email ?? "your email";
        ReturnUrl = state.ReturnUrl ?? uiOptions.DefaultReturnUrl;
        return state;
    }

    public sealed class InputModel
    {
        [Required]
        public string Code { get; set; } = string.Empty;
    }
}
