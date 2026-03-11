# Incursa.Platform.Access.Razor

`Incursa.Platform.Access.Razor` is the provider-neutral Razor Pages UI layer for `Incursa.Platform.Access`.

It packages the shared authentication pages, layout, static assets, redirect state handling, and helper endpoints so product apps do not need to copy auth UI into each host.

## Responsibilities

The package owns:

- Razor Pages for sign-in, callback, magic auth, email verification, MFA, organization selection, password recovery, and terminal auth states
- the shared auth shell layout `_AccessAuthLayout`
- auth-specific static web assets
- redirect and pending-challenge state persistence
- provider button presentation helpers
- provider-neutral branding, route, and setup options
- login and logout endpoint helpers

The package does not own:

- provider protocol calls
- cookie and ticket issuance beyond `Incursa.Platform.Access.AspNetCore`
- provider-specific password reset APIs
- app-specific post-auth landing rules such as local organization selection

## Quick Start

Consume the package through a local `ProjectReference` while developing:

```xml
<ItemGroup>
  <ProjectReference Include="..\..\..\platform\src\Incursa.Platform.Access.Razor\Incursa.Platform.Access.Razor.csproj" />
  <ProjectReference Include="..\..\..\platform\src\Incursa.Integrations.WorkOS.AspNetCore\Incursa.Integrations.WorkOS.AspNetCore.csproj" />
</ItemGroup>
```

Replace those with `PackageReference`s once the packages are published.

Register the reusable UI first, then plug in the provider adapter:

```csharp
using Incursa.Integrations.WorkOS;
using Incursa.Platform.Access.Razor;

builder.Services.AddRazorPages();

builder.Services.AddAccessAuthenticationUi(options =>
{
    options.AuthenticationScheme = "relay-ui";
    options.DefaultReturnUrl = "/relay";
    options.PublicBaseUrl = configuration["WorkOS:PublicBaseUrl"];
    options.IsConfigured = true;

    options.Branding.ApplicationName = "Request Relay";
    options.Branding.Eyebrow = "Sign in";
    options.Branding.Headline = "Open the relay UI.";
    options.Branding.SidebarLabel = "Request Relay";
    options.Branding.SidebarHeadline = "Secure relay operations, with the auth flow kept inside the product.";
    options.Branding.SidebarNote = "Authentication happens through the configured provider while the UI package keeps the browser flow, verification, and organization context in one place.";

    options.Setup.BadgeText = "Setup required";
    options.Setup.Title = "Authentication is not configured.";
    options.Setup.Description = "This deployment can host a custom browser sign-in flow, but it still needs provider credentials before the flow can start.";
    options.Setup.RequiredConfigurationEntries =
    [
        "WorkOS__ClientId",
        "WorkOS__ClientSecret",
        "WorkOS__ApiKey",
    ];
});

builder.Services.AddWorkOsCustomUiAuthentication(
    configureAuth: options =>
    {
        options.ClientId = configuration["WorkOS:ClientId"]!;
        options.ClientSecret = configuration["WorkOS:ClientSecret"]!;
        options.ApiKey = configuration["WorkOS:ApiKey"]!;
    },
    configureCookie: options =>
    {
        options.AuthenticationScheme = "relay-ui";
    });

var app = builder.Build();

app.MapAccessAuthenticationUiEndpoints();
app.MapRazorPages();
```

If the deployment is intentionally unconfigured, register the fallback package services instead:

```csharp
builder.Services.AddAccessAuthenticationUi();
builder.Services.AddUnavailableAccessAuthenticationUi("Authentication is not configured.");
```

## Shared Surface

Default packaged page routes:

- `/auth/sign-in`
- `/auth/callback`
- `/auth/magic`
- `/auth/magic/verify`
- `/auth/verify-email`
- `/auth/mfa/setup`
- `/auth/mfa/verify`
- `/auth/organizations/select`
- `/auth/password/forgot`
- `/auth/password/reset`
- `/auth/error`
- `/auth/access-denied`
- `/auth/logged-out`
- `/auth/session-expired`

Shared helper endpoints:

- `/auth/login`
- `/auth/logout`
- `/auth/sign-out`

Shared layout and assets:

- layout: `_AccessAuthLayout`
- stylesheet: `/_content/Incursa.Platform.Access.Razor/css/access-auth.css`
- script: `/_content/Incursa.Platform.Access.Razor/js/access-auth.js`

`AccessAuthenticationUiOptions.Routes` controls helper endpoint targets and also adds alternate Razor Page routes when you need host-specific aliases. The packaged `/auth/*` routes remain available as the built-in defaults.

## Password Recovery

Password recovery is exposed through the optional `IAccessPasswordRecoveryService` seam.

That keeps the base UI package provider-neutral while still allowing a provider adapter to supply a working reset flow. `Incursa.Integrations.WorkOS.AspNetCore` registers a WorkOS-backed implementation automatically when you call `AddWorkOsCustomUiAuthentication(...)`.

## Host Responsibilities

The host application still decides:

- whether a provider is configured for the current deployment
- the authentication scheme and cookie names
- the product branding text
- post-auth routing rules such as local organization selection
- authorization policies around protected app areas

The host should not need to carry the shared auth pages, layout, or provider-specific recovery client directly.

## RequestRelay Status

`requestrelay` now consumes this package through a local project reference and no longer carries its own auth pages or vendored WorkOS browser auth client copy. The remaining `/signup` page in requestrelay is app-specific local organization selection, not shared provider auth UI.

## Build And Pack

Validate the package contract locally with:

```powershell
dotnet build src/Incursa.Platform.Access.Razor/Incursa.Platform.Access.Razor.csproj -c Debug
dotnet test tests/Incursa.Platform.Access.AspNetCore.Tests/Incursa.Platform.Access.AspNetCore.Tests.csproj -c Debug --no-build
dotnet pack src/Incursa.Platform.Access.Razor/Incursa.Platform.Access.Razor.csproj -c Debug
dotnet pack src/Incursa.Integrations.WorkOS.AspNetCore/Incursa.Integrations.WorkOS.AspNetCore.csproj -c Debug
```

See [../../docs/access-auth-ui-architecture.md](../../docs/access-auth-ui-architecture.md) for the layering and publish guidance.
