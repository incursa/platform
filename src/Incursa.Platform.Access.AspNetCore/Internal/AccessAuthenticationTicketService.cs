namespace Incursa.Platform.Access.AspNetCore;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

internal sealed class AccessAuthenticationTicketService : IAccessAuthenticationTicketService
{
    private readonly IAccessClaimsPrincipalFactory principalFactory;
    private readonly IAccessSessionStore sessionStore;
    private readonly IAccessAuthenticationService? authenticationService;
    private readonly AccessSessionCookieOptions options;

    public AccessAuthenticationTicketService(
        IAccessClaimsPrincipalFactory principalFactory,
        IAccessSessionStore sessionStore,
        IOptions<AccessSessionCookieOptions> options,
        IEnumerable<IAccessAuthenticationService> authenticationServices)
    {
        this.principalFactory = principalFactory ?? throw new ArgumentNullException(nameof(principalFactory));
        this.sessionStore = sessionStore ?? throw new ArgumentNullException(nameof(sessionStore));
        ArgumentNullException.ThrowIfNull(options);
        this.options = options.Value;
        ArgumentNullException.ThrowIfNull(authenticationServices);
        authenticationService = authenticationServices.SingleOrDefault();
    }

    public async Task SignInAsync(
        HttpContext httpContext,
        AccessAuthenticatedSession session,
        AuthenticationProperties? properties = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(session);

        cancellationToken.ThrowIfCancellationRequested();

        await sessionStore.SetAsync(session, cancellationToken).ConfigureAwait(false);

        properties ??= new AuthenticationProperties
        {
            AllowRefresh = true,
            IsPersistent = true,
            ExpiresUtc = session.RefreshTokenExpiresAtUtc
                ?? session.AccessTokenExpiresAtUtc
                ?? DateTimeOffset.UtcNow.Add(options.ExpireTimeSpan),
        };

        await httpContext.SignInAsync(
            options.AuthenticationScheme,
            principalFactory.CreatePrincipal(session),
            properties).ConfigureAwait(false);
    }

    public async Task<AccessSignOutResult> SignOutAsync(
        HttpContext httpContext,
        AccessSignOutRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        cancellationToken.ThrowIfCancellationRequested();

        var currentSession = await sessionStore.GetAsync(cancellationToken).ConfigureAwait(false);
        var effectiveRequest = request ?? new AccessSignOutRequest(currentSession?.SessionId);

        var result = authenticationService is null
            ? new AccessSignOutResult(false)
            : await authenticationService.SignOutAsync(
                new AccessSignOutRequest(
                    effectiveRequest.SessionId ?? currentSession?.SessionId,
                    effectiveRequest.ReturnToUri),
                cancellationToken).ConfigureAwait(false);

        await sessionStore.ClearAsync(cancellationToken).ConfigureAwait(false);
        await httpContext.SignOutAsync(options.AuthenticationScheme).ConfigureAwait(false);
        return result;
    }
}
