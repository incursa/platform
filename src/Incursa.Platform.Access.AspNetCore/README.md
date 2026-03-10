# Incursa.Platform.Access.AspNetCore

`Incursa.Platform.Access.AspNetCore` is the ASP.NET Core hosting adapter for the `Incursa.Platform.Access` capability. It resolves the current request’s access context without introducing a second identity or permission model.

## What It Owns

- current request access context resolution
- claim, route, and query mapping into the existing access capability
- personal-scope and organization-scope resolution against `IAccessQueryService`
- secure default cookie-backed session persistence for `AccessAuthenticatedSession`
- sign-in/sign-out helpers for issuing and clearing the app's local auth cookie

## What It Does Not Own

- the canonical access registry or access state model
- generic ASP.NET authorization policy plumbing
- provider-specific session or management APIs

## Related Packages

- `Incursa.Platform.Access` for the source-of-truth access model
- `Incursa.Integrations.WorkOS.AspNetCore` when request context originates from WorkOS session or claim material
- `Incursa.Integrations.WorkOS.Access` when WorkOS organizations and roles need to synchronize into the local access model

## Typical Use

```csharp
services.AddAccess(registry =>
{
    registry.AddPermission("tenant.read", "Read tenant");
    registry.AddRole("organization-member", "Organization member", "tenant.read");
});

services.AddAccessAspNetCore(options =>
{
    options.ScopeRootExternalLinkProvider = "workos";
    options.ScopeRootExternalLinkResourceType = "organization";
});

services.AddAccessCookieAuthentication(options =>
{
    options.AuthenticationScheme = "Access";
});
```

Resolve the current request context from DI:

```csharp
var accessContext = await accessor.GetCurrentAsync(cancellationToken);
```

## Session And Ticket Helpers

- `ICurrentAccessContextAccessor` resolves and caches the current access context once per request
- `IAccessSessionStore` defaults to an encrypted, HttpOnly cookie-backed implementation
- `IAccessAuthenticationTicketService` signs the local app in/out and coordinates local cookie/session cleanup

Typical sign-in flow after a successful `IAccessAuthenticationService` call:

```csharp
var outcome = await authenticationService.SignInWithPasswordAsync(request, cancellationToken);

if (outcome is AccessAuthenticationSucceeded success)
{
    await ticketService.SignInAsync(HttpContext, success.Session, cancellationToken: cancellationToken);
}
```

Typical sign-out flow:

```csharp
var signOut = await ticketService.SignOutAsync(HttpContext, cancellationToken: cancellationToken);
```

If the underlying authentication provider can revoke a remote session, the returned `AccessSignOutResult` includes that status and any provider logout URL.
