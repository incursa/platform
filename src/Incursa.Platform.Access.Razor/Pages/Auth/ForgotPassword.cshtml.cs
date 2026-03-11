namespace Incursa.Platform.Access.Razor.Pages.Auth;

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

[AllowAnonymous]
public sealed class ForgotPasswordModel : PageModel
{
    private readonly IAccessPasswordRecoveryService passwordRecoveryService;
    private readonly AccessAuthenticationUiOptions uiOptions;
    private readonly ILogger<ForgotPasswordModel> logger;

    public ForgotPasswordModel(
        IAccessPasswordRecoveryService passwordRecoveryService,
        IOptions<AccessAuthenticationUiOptions> uiOptions,
        ILogger<ForgotPasswordModel> logger)
    {
        this.passwordRecoveryService = passwordRecoveryService ?? throw new ArgumentNullException(nameof(passwordRecoveryService));
        ArgumentNullException.ThrowIfNull(uiOptions);
        this.uiOptions = uiOptions.Value;
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

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

        if (!uiOptions.IsConfigured || !uiOptions.EnablePassword || !uiOptions.EnablePasswordRecovery)
        {
            return LocalRedirect(SignInPath);
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        ReturnUrl = AccessAuthenticationRequestHelpers.NormalizeReturnUrl(ReturnUrl) ?? uiOptions.DefaultReturnUrl;

        if (!uiOptions.IsConfigured || !uiOptions.EnablePassword || !uiOptions.EnablePasswordRecovery)
        {
            return LocalRedirect(SignInPath);
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            var result = await passwordRecoveryService
                .RequestResetAsync(
                    new AccessPasswordResetRequest(
                        Input.Email,
                        ReturnUrl,
                        AccessAuthenticationRequestHelpers.BuildMetadata(HttpContext)),
                    cancellationToken)
                .ConfigureAwait(false);

            if (!result.Accepted)
            {
                ModelState.AddModelError(string.Empty, result.Message ?? "The password reset request could not be started. Try again.");
                return Page();
            }

            StatusMessage = string.IsNullOrWhiteSpace(result.Message)
                ? "If the account exists, a password reset email is on the way."
                : result.Message;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Password reset request failed for {Email}.", Input.Email);
            ModelState.AddModelError(string.Empty, "The password reset request could not be started. Try again.");
            return Page();
        }

        return RedirectToPage("/Auth/ForgotPassword", new { returnUrl = ReturnUrl });
    }

    public sealed class InputModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}
