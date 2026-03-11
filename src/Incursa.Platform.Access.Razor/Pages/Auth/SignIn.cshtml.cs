namespace Incursa.Platform.Access.Razor.Pages.Auth;

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

[AllowAnonymous]
public sealed class SignInModel : PageModel
{
    private readonly IAccessAuthenticationService authenticationService;
    private readonly AccessAuthenticationFlowRouter flowRouter;
    private readonly AccessAuthenticationStateStore stateStore;
    private readonly AccessAuthenticationUiOptions uiOptions;

    public SignInModel(
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

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    [TempData]
    public string? StatusMessage { get; set; }

    public bool IsConfigured => uiOptions.IsConfigured;

    public bool PasswordEnabled => uiOptions.EnablePassword;

    public bool PasswordRecoveryEnabled => PasswordEnabled && uiOptions.EnablePasswordRecovery;

    public bool MagicAuthEnabled => uiOptions.EnableMagicAuth;

    public AccessAuthenticationSetupOptions Setup => uiOptions.Setup;

    public AccessAuthenticationBrandingOptions Branding => uiOptions.Branding;

    public string MagicPath => AccessAuthenticationUiEndpointRouteBuilderExtensions.AppendReturnUrl(uiOptions.Routes.MagicPath, ReturnUrl);

    public string ForgotPasswordPath => AccessAuthenticationUiEndpointRouteBuilderExtensions.AppendReturnUrl(uiOptions.Routes.ForgotPasswordPath, ReturnUrl);

    public IReadOnlyList<AuthProviderViewModel> Providers { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        ReturnUrl = AccessAuthenticationRequestHelpers.NormalizeReturnUrl(ReturnUrl) ?? uiOptions.DefaultReturnUrl;
        var principal = await HttpContext.GetAccessAuthenticationUiPrincipalAsync().ConfigureAwait(false);
        if (principal?.Identity?.IsAuthenticated == true)
        {
            return LocalRedirect(ReturnUrl);
        }

        LoadProviders();
        return Page();
    }

    public async Task<IActionResult> OnPostPasswordAsync(CancellationToken cancellationToken)
    {
        LoadProviders();
        ReturnUrl = AccessAuthenticationRequestHelpers.NormalizeReturnUrl(ReturnUrl) ?? uiOptions.DefaultReturnUrl;

        if (!IsConfigured)
        {
            ModelState.AddModelError(string.Empty, uiOptions.Setup.Title);
            return Page();
        }

        if (!PasswordEnabled)
        {
            ModelState.AddModelError(string.Empty, "Password sign-in is disabled for this deployment.");
            return Page();
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var outcome = await authenticationService
            .SignInWithPasswordAsync(
                new AccessPasswordSignInRequest(
                    Input.Email,
                    Input.Password,
                    AccessAuthenticationRequestHelpers.BuildMetadata(HttpContext)),
                cancellationToken)
            .ConfigureAwait(false);

        var handled = await flowRouter.HandleAsync(HttpContext, outcome, ReturnUrl, cancellationToken).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(handled.RedirectUri))
        {
            return LocalRedirect(handled.RedirectUri);
        }

        ModelState.AddModelError(string.Empty, handled.ErrorMessage ?? "Authentication could not be completed.");
        return Page();
    }

    public async Task<IActionResult> OnPostProviderAsync(int index, CancellationToken cancellationToken)
    {
        LoadProviders();
        ReturnUrl = AccessAuthenticationRequestHelpers.NormalizeReturnUrl(ReturnUrl) ?? uiOptions.DefaultReturnUrl;

        if (!IsConfigured)
        {
            return LocalRedirect(uiOptions.Routes.SignInPath);
        }

        if (index < 0 || index >= Providers.Count)
        {
            ModelState.AddModelError(string.Empty, "The selected provider is not available.");
            return Page();
        }

        var provider = Providers[index].Options;
        var state = stateStore.CreateRedirectState(ReturnUrl);
        stateStore.SaveRedirectState(HttpContext, state);

        var callbackUrl = AccessAuthenticationRequestHelpers.BuildAppAbsoluteUrl(
            Request,
            uiOptions.PublicBaseUrl,
            uiOptions.Routes.CallbackPath);
        var redirect = await authenticationService
            .CreateAuthorizationUrlAsync(
                new AccessRedirectAuthorizationRequest(
                    callbackUrl,
                    provider.Provider,
                    provider.ConnectionId,
                    provider.OrganizationId,
                    state: state.State),
                cancellationToken)
            .ConfigureAwait(false);

        return Redirect(redirect.Url.ToString());
    }

    private void LoadProviders()
    {
        Providers = uiOptions.Providers
            .Where(static provider =>
                !string.IsNullOrWhiteSpace(provider.Provider)
                || !string.IsNullOrWhiteSpace(provider.ConnectionId))
            .Select(static (provider, index) => AuthProviderViewModel.Create(provider, index))
            .ToArray();
    }

    public sealed class AuthProviderViewModel
    {
        private AuthProviderViewModel(
            AccessAuthenticationProviderOptions options,
            int index,
            AccessAuthenticationProviderPresentation presentation)
        {
            Options = options;
            Index = index;
            DisplayLabel = presentation.DisplayLabel;
            Description = presentation.Description;
            ThemeClass = presentation.ThemeClass;
            IconSvg = presentation.IconSvg;
        }

        public AccessAuthenticationProviderOptions Options { get; }

        public int Index { get; }

        public string DisplayLabel { get; }

        public string Description { get; }

        public string ThemeClass { get; }

        public string IconSvg { get; }

        public static AuthProviderViewModel Create(AccessAuthenticationProviderOptions options, int index) =>
            new(options, index, AccessAuthenticationProviderPresentation.Create(options));
    }

    public sealed class InputModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }
}
