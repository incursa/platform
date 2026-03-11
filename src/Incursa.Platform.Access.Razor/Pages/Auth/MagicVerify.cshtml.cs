namespace Incursa.Platform.Access.Razor.Pages.Auth;

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

[AllowAnonymous]
public sealed class MagicVerifyModel : PageModel
{
    private readonly IAccessAuthenticationService authenticationService;
    private readonly AccessAuthenticationFlowRouter flowRouter;
    private readonly AccessAuthenticationUiOptions uiOptions;

    public MagicVerifyModel(
        IAccessAuthenticationService authenticationService,
        AccessAuthenticationFlowRouter flowRouter,
        IOptions<AccessAuthenticationUiOptions> uiOptions)
    {
        this.authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
        this.flowRouter = flowRouter ?? throw new ArgumentNullException(nameof(flowRouter));
        ArgumentNullException.ThrowIfNull(uiOptions);
        this.uiOptions = uiOptions.Value;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    [BindProperty(SupportsGet = true, Name = "sent")]
    public bool CodeWasSent { get; set; }

    [TempData]
    public string? StatusMessage { get; set; }

    public string SignInPath => AccessAuthenticationUiEndpointRouteBuilderExtensions.AppendReturnUrl(uiOptions.Routes.SignInPath, ReturnUrl);

    public async Task<IActionResult> OnGetAsync()
    {
        ReturnUrl = AccessAuthenticationRequestHelpers.NormalizeReturnUrl(ReturnUrl) ?? uiOptions.DefaultReturnUrl;
        var principal = await HttpContext.GetAccessAuthenticationUiPrincipalAsync().ConfigureAwait(false);
        if (principal?.Identity?.IsAuthenticated == true)
        {
            return LocalRedirect(ReturnUrl);
        }

        if (!uiOptions.EnableMagicAuth)
        {
            return LocalRedirect(SignInPath);
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        ReturnUrl = AccessAuthenticationRequestHelpers.NormalizeReturnUrl(ReturnUrl) ?? uiOptions.DefaultReturnUrl;

        if (!uiOptions.IsConfigured || !uiOptions.EnableMagicAuth)
        {
            return LocalRedirect(SignInPath);
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var outcome = await authenticationService
            .CompleteMagicAuthAsync(
                new AccessMagicAuthCompletionRequest(
                    Input.Code,
                    AccessAuthenticationRequestHelpers.BuildMetadata(HttpContext)),
                cancellationToken)
            .ConfigureAwait(false);

        var handled = await flowRouter.HandleAsync(HttpContext, outcome, ReturnUrl, cancellationToken).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(handled.RedirectUri))
        {
            return LocalRedirect(handled.RedirectUri);
        }

        ModelState.AddModelError(string.Empty, handled.ErrorMessage ?? "The verification code was not accepted.");
        return Page();
    }

    public async Task<IActionResult> OnPostResendAsync(CancellationToken cancellationToken)
    {
        ReturnUrl = AccessAuthenticationRequestHelpers.NormalizeReturnUrl(ReturnUrl) ?? uiOptions.DefaultReturnUrl;

        if (!uiOptions.IsConfigured || !uiOptions.EnableMagicAuth)
        {
            return LocalRedirect(SignInPath);
        }

        if (string.IsNullOrWhiteSpace(Email))
        {
            ModelState.AddModelError(string.Empty, "Enter an email address before requesting another code.");
            return Page();
        }

        await authenticationService
            .BeginMagicAuthAsync(new AccessMagicAuthStartRequest(Email) { ReturnCode = false }, cancellationToken)
            .ConfigureAwait(false);

        StatusMessage = "A fresh code was sent.";
        return RedirectToPage("/Auth/MagicVerify", new { email = Email, returnUrl = ReturnUrl });
    }

    public sealed class InputModel
    {
        [Required]
        public string Code { get; set; } = string.Empty;
    }
}
