# Incursa.Platform.Access.Razor

`Incursa.Platform.Access.Razor` is the provider-neutral Razor UI layer for `Incursa.Platform.Access`.

It exists to keep reusable authentication screens, flow-state handling, and host-facing UI configuration out of individual applications while still allowing different authentication providers to drive the same experience.

## What It Owns

- reusable auth-flow infrastructure for Razor Pages hosts
- protected redirect/challenge state handling for multi-step sign-in flows
- provider button metadata and presentation helpers for branded sign-in choices
- provider-neutral route and branding options for auth UI packaging
- the extension seam for optional password recovery flows that are not universal across providers

## What It Does Not Own

- provider-specific authentication protocol calls
- cookie/session ticket issuance beyond `Incursa.Platform.Access.AspNetCore`
- WorkOS-specific endpoints, middleware, or token exchange behavior
- the canonical access model in `Incursa.Platform.Access`

## Usage

Register the provider-neutral access packages and your provider adapter first, then add the reusable auth UI services:

```csharp
services.AddAccess();

services.AddAccessCookieAuthentication(options =>
{
    options.AuthenticationScheme = "Access";
});

services.AddWorkOsCustomUiAuthentication(
    configureAuth: options =>
    {
        options.ClientId = builder.Configuration["WorkOs:ClientId"]!;
        options.ClientSecret = builder.Configuration["WorkOs:ClientSecret"]!;
        options.ApiKey = builder.Configuration["WorkOs:ApiKey"]!;
    });

services.AddAccessAuthenticationUi(options =>
{
    options.DefaultReturnUrl = "/relay";
    options.Branding.ApplicationName = "Request Relay";
    options.TotpIssuer = "Incursa Request Relay";
    options.Providers.Add(new AccessAuthenticationProviderOptions
    {
        Label = "Google",
        Provider = "google",
    });
});
```

Use the shared flow router from a page model or endpoint after any `IAccessAuthenticationService` call:

```csharp
var outcome = await authenticationService.SignInWithPasswordAsync(request, cancellationToken);
var handled = await flowRouter.HandleAsync(HttpContext, outcome, returnUrl, cancellationToken);
```

## Password Recovery

Password reset and recovery are intentionally modeled as an optional seam through `IAccessPasswordRecoveryService`.

Some providers can support a complete self-service recovery flow, others cannot, and some applications may choose to keep those screens disabled. The reusable UI package should not hard-code a WorkOS-specific recovery client into its base contracts.

## Migration Path

This package is the extraction target for the current requestrelay auth screens. The intended sequence is:

1. stabilize shared auth-flow infrastructure here
2. move shared Razor pages, layouts, and assets here
3. keep provider adapters thin and app-owned text/theme overrides configurable
4. let apps override copy, colors, and route placement without forking the flow logic

See [docs/access-auth-ui-architecture.md](../../docs/access-auth-ui-architecture.md) for the detailed packaging plan.
