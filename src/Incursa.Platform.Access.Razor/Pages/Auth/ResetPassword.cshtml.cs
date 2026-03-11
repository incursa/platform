namespace Incursa.Platform.Access.Razor.Pages.Auth;

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

[AllowAnonymous]
public sealed class ResetPasswordModel : PageModel
{
    private readonly IAccessPasswordRecoveryService passwordRecoveryService;
    private readonly AccessAuthenticationUiOptions uiOptions;
    private readonly ILogger<ResetPasswordModel> logger;

    public ResetPasswordModel(
        IAccessPasswordRecoveryService passwordRecoveryService,
        IOptions<AccessAuthenticationUiOptions> uiOptions,
        ILogger<ResetPasswordModel> logger)
    {
        this.passwordRecoveryService = passwordRecoveryService ?? throw new ArgumentNullException(nameof(passwordRecoveryService));
        ArgumentNullException.ThrowIfNull(uiOptions);
        this.uiOptions = uiOptions.Value;
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [BindProperty(SupportsGet = true)]
    public string Token { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [TempData]
    public string? StatusMessage { get; set; }

    public string SignInPath => AccessAuthenticationUiEndpointRouteBuilderExtensions.AppendReturnUrl(uiOptions.Routes.SignInPath, ReturnUrl);

    public IActionResult OnGet()
    {
        ReturnUrl = AccessAuthenticationRequestHelpers.NormalizeReturnUrl(ReturnUrl) ?? uiOptions.DefaultReturnUrl;

        if (!uiOptions.IsConfigured || !uiOptions.EnablePassword || !uiOptions.EnablePasswordRecovery)
        {
            return LocalRedirect(SignInPath);
        }

        if (string.IsNullOrWhiteSpace(Token))
        {
            return LocalRedirect(QueryHelpers.AddQueryString(
                uiOptions.Routes.ErrorPath,
                new Dictionary<string, string?>(StringComparer.Ordinal)
                {
                    ["code"] = "missing-reset-token",
                    ["description"] = "The password reset link is incomplete. Request a fresh reset email.",
                }));
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

        if (string.IsNullOrWhiteSpace(Token))
        {
            ModelState.AddModelError(string.Empty, "The password reset token is missing. Request a fresh reset email.");
            return Page();
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            var result = await passwordRecoveryService
                .ResetPasswordAsync(
                    new AccessPasswordResetCompletionRequest(
                        Token,
                        Input.NewPassword,
                        AccessAuthenticationRequestHelpers.BuildMetadata(HttpContext)),
                    cancellationToken)
                .ConfigureAwait(false);

            if (!result.Accepted)
            {
                ModelState.AddModelError(string.Empty, result.Message ?? "The password could not be updated from this link. Request a fresh reset email.");
                return Page();
            }

            StatusMessage = string.IsNullOrWhiteSpace(result.Message)
                ? "Password updated. Sign in with your new password."
                : result.Message;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Password reset confirmation failed.");
            ModelState.AddModelError(string.Empty, "The password could not be updated from this link. Request a fresh reset email.");
            return Page();
        }

        return LocalRedirect(SignInPath);
    }

    public sealed class InputModel
    {
        [Required]
        [DataType(DataType.Password)]
        [MinLength(8)]
        [Display(Name = "New password")]
        public string NewPassword { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare(nameof(NewPassword), ErrorMessage = "The password confirmation must match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
