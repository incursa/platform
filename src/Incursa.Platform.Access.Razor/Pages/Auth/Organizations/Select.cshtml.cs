namespace Incursa.Platform.Access.Razor.Pages.Auth.Organizations;

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

[AllowAnonymous]
public sealed class SelectModel : PageModel
{
    private readonly IAccessAuthenticationService authenticationService;
    private readonly AccessAuthenticationFlowRouter flowRouter;
    private readonly AccessAuthenticationStateStore stateStore;
    private readonly AccessAuthenticationUiOptions uiOptions;

    public SelectModel(
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
    [Required]
    public string SelectedOrganizationId { get; set; } = string.Empty;

    public string? Email { get; private set; }

    public string ReturnUrl { get; private set; } = "/";

    public string SignInPath => AccessAuthenticationUiEndpointRouteBuilderExtensions.AppendReturnUrl(uiOptions.Routes.SignInPath, ReturnUrl);

    public IReadOnlyList<AccessPendingOrganizationChoice> Organizations { get; private set; } = [];

    public IActionResult OnGet()
    {
        return LoadState() is null ? LocalRedirect(uiOptions.Routes.SessionExpiredPath) : Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var state = LoadState();
        if (state is null)
        {
            return LocalRedirect(uiOptions.Routes.SessionExpiredPath);
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        if (!state.ContainsOrganization(SelectedOrganizationId))
        {
            ModelState.AddModelError(nameof(SelectedOrganizationId), "Select one of the organizations returned by the authentication provider.");
            return Page();
        }

        var outcome = await authenticationService
            .CompleteOrganizationSelectionAsync(
                new AccessOrganizationSelectionRequest(
                    state.PendingAuthenticationToken,
                    SelectedOrganizationId,
                    AccessAuthenticationRequestHelpers.BuildMetadata(HttpContext)),
                cancellationToken)
            .ConfigureAwait(false);

        var handled = await flowRouter.HandleAsync(HttpContext, outcome, state.ReturnUrl, cancellationToken).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(handled.RedirectUri))
        {
            return LocalRedirect(handled.RedirectUri);
        }

        ModelState.AddModelError(string.Empty, handled.ErrorMessage ?? "That organization could not be selected.");
        return Page();
    }

    private AccessPendingAuthenticationState? LoadState()
    {
        var state = stateStore.GetPendingChallenge(HttpContext);
        if (state is null || state.Kind != AccessChallengeKind.OrganizationSelectionRequired)
        {
            return null;
        }

        Organizations = state.Organizations;
        Email = state.Email;
        ReturnUrl = state.ReturnUrl ?? uiOptions.DefaultReturnUrl;
        return state;
    }
}
