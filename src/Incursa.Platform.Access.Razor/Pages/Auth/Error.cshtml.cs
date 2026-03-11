namespace Incursa.Platform.Access.Razor.Pages.Auth;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

[AllowAnonymous]
public sealed class ErrorModel : PageModel
{
    private readonly AccessAuthenticationUiOptions uiOptions;

    public ErrorModel(IOptions<AccessAuthenticationUiOptions> uiOptions)
    {
        ArgumentNullException.ThrowIfNull(uiOptions);
        this.uiOptions = uiOptions.Value;
    }

    [BindProperty(SupportsGet = true)]
    public string? Code { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Description { get; set; }

    public string Title { get; private set; } = "Authentication failed.";

    public string Message { get; private set; } = "The sign-in flow could not be completed. Start again from the sign-in page.";

    public string SignInPath => uiOptions.Routes.SignInPath;

    public void OnGet()
    {
        var normalizedCode = Code?.Trim().ToLowerInvariant();
        switch (normalizedCode)
        {
            case "invalid-state":
                Title = "This sign-in request is no longer valid.";
                Message = "The provider callback could not be matched to the browser that started it. Start the sign-in flow again.";
                break;

            case "missing-code":
                Title = "The provider did not return an authorization code.";
                Message = "The callback was incomplete, so the app could not finish signing you in.";
                break;

            case "missing-reset-token":
                Title = "That password reset link is incomplete.";
                Message = "Request a fresh reset email, then open the newest link.";
                break;

            case "unsupported-challenge":
                Title = "This challenge needs an additional flow.";
                Message = "The authentication provider returned a challenge this UI does not surface directly yet. Start again or contact an operator.";
                break;

            case "callback-failed":
                Title = "The callback could not complete.";
                Message = string.IsNullOrWhiteSpace(Description)
                    ? "The provider returned a response, but the app could not finish creating the session."
                    : Description.Trim();
                break;

            default:
                if (!string.IsNullOrWhiteSpace(Description))
                {
                    Message = Description.Trim();
                }

                break;
        }
    }
}
