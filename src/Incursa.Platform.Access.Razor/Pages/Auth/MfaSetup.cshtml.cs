namespace Incursa.Platform.Access.Razor.Pages.Auth;

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

[AllowAnonymous]
public sealed class MfaSetupModel : PageModel
{
    private readonly IAccessAuthenticationService authenticationService;
    private readonly AccessAuthenticationFlowRouter flowRouter;
    private readonly AccessAuthenticationStateStore stateStore;
    private readonly AccessAuthenticationUiOptions uiOptions;

    public MfaSetupModel(
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

    public string ReturnUrl { get; private set; } = "/";

    public string Issuer { get; private set; } = string.Empty;

    public string UserLabel { get; private set; } = string.Empty;

    public string? QrCode { get; private set; }

    public string Secret { get; private set; } = string.Empty;

    public string SignInPath => AccessAuthenticationUiEndpointRouteBuilderExtensions.AppendReturnUrl(uiOptions.Routes.SignInPath, ReturnUrl);

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var state = stateStore.GetPendingChallenge(HttpContext);
        if (state is null || state.Kind != AccessChallengeKind.MfaEnrollmentRequired)
        {
            return LocalRedirect(uiOptions.Routes.SessionExpiredPath);
        }

        await LoadEnrollmentAsync(state, cancellationToken).ConfigureAwait(false);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var state = stateStore.GetPendingChallenge(HttpContext);
        if (state is null || state.Kind != AccessChallengeKind.MfaEnrollmentRequired)
        {
            return LocalRedirect(uiOptions.Routes.SessionExpiredPath);
        }

        if (string.IsNullOrWhiteSpace(state.AuthenticationFactorId))
        {
            ModelState.AddModelError(string.Empty, "Refresh the page and scan the authenticator setup again.");
            await LoadEnrollmentAsync(state, cancellationToken).ConfigureAwait(false);
            return Page();
        }

        if (!ModelState.IsValid)
        {
            await LoadEnrollmentAsync(state, cancellationToken).ConfigureAwait(false);
            return Page();
        }

        var outcome = await authenticationService
            .CompleteTotpAsync(
                new AccessTotpCompletionRequest(
                    state.PendingAuthenticationToken,
                    state.AuthenticationFactorId,
                    code: Input.Code,
                    enrollmentIssuer: ResolveIssuer(),
                    enrollmentUser: ResolveUserLabel(state),
                    metadata: AccessAuthenticationRequestHelpers.BuildMetadata(HttpContext)),
                cancellationToken)
            .ConfigureAwait(false);

        var handled = await flowRouter.HandleAsync(HttpContext, outcome, state.ReturnUrl, cancellationToken).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(handled.RedirectUri))
        {
            return LocalRedirect(handled.RedirectUri);
        }

        ModelState.AddModelError(string.Empty, handled.ErrorMessage ?? "The MFA code was not accepted.");
        await LoadEnrollmentAsync(state, cancellationToken).ConfigureAwait(false);
        return Page();
    }

    private async Task LoadEnrollmentAsync(AccessPendingAuthenticationState state, CancellationToken cancellationToken)
    {
        Issuer = ResolveIssuer();
        UserLabel = ResolveUserLabel(state);
        ReturnUrl = state.ReturnUrl ?? uiOptions.DefaultReturnUrl;

        var enrollment = await authenticationService
            .EnrollTotpAsync(new AccessTotpEnrollmentRequest(Issuer, UserLabel), cancellationToken)
            .ConfigureAwait(false);

        QrCode = enrollment.QrCode;
        Secret = enrollment.Secret ?? string.Empty;
        stateStore.SavePendingChallenge(HttpContext, state with { AuthenticationFactorId = enrollment.FactorId });
    }

    private string ResolveIssuer() =>
        string.IsNullOrWhiteSpace(uiOptions.TotpIssuer)
            ? "Incursa"
            : uiOptions.TotpIssuer.Trim();

    private static string ResolveUserLabel(AccessPendingAuthenticationState state) =>
        string.IsNullOrWhiteSpace(state.Email)
            ? "operator"
            : state.Email.Trim();

    public sealed class InputModel
    {
        [Required]
        public string Code { get; set; } = string.Empty;
    }
}
