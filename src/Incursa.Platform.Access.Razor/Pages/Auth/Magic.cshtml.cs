namespace Incursa.Platform.Access.Razor.Pages.Auth;

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

[AllowAnonymous]
public sealed class MagicModel : PageModel
{
    private readonly IAccessAuthenticationService authenticationService;
    private readonly AccessAuthenticationUiOptions uiOptions;

    public MagicModel(
        IAccessAuthenticationService authenticationService,
        IOptions<AccessAuthenticationUiOptions> uiOptions)
    {
        this.authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
        ArgumentNullException.ThrowIfNull(uiOptions);
        this.uiOptions = uiOptions.Value;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

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

        await authenticationService
            .BeginMagicAuthAsync(new AccessMagicAuthStartRequest(Input.Email) { ReturnCode = false }, cancellationToken)
            .ConfigureAwait(false);

        return RedirectToPage("/Auth/MagicVerify", new
        {
            email = Input.Email,
            returnUrl = ReturnUrl,
            sent = true,
        });
    }

    public sealed class InputModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}
