# Access Auth UI Architecture

## Decision

The reusable browser authentication experience lives in `Incursa.Platform.Access.Razor`.

That package is provider-neutral and sits beside the existing access contracts and ASP.NET Core hosting helpers. Provider packages such as WorkOS plug into it through `IAccessAuthenticationService`, `IAccessAuthenticationTicketService`, and the optional `IAccessPasswordRecoveryService` seam.

This is the right first reuse shape because it preserves the existing Razor Pages implementation, keeps the UI close to ASP.NET Core, and avoids baking WorkOS-specific assumptions into the shared package.

## Layering

`Incursa.Platform.Access`

- access contracts
- challenge/result models
- authenticated session models

`Incursa.Platform.Access.AspNetCore`

- cookie-backed local session persistence
- ticket issuance and sign-out helpers
- current request access context helpers

`Incursa.Platform.Access.Razor`

- auth Razor Pages
- auth shell layout and static assets
- redirect state and pending challenge persistence
- provider button presentation
- provider-neutral setup, branding, and route options
- helper endpoints for login and logout
- optional password recovery seam

Provider adapters such as `Incursa.Integrations.WorkOS.Access` and `Incursa.Integrations.WorkOS.AspNetCore`

- provider protocol calls
- provider-specific configuration
- provider-specific recovery adapters
- provider-specific host registration helpers

## Current State

The extraction is complete enough for local consumption and package validation.

`Incursa.Platform.Access.Razor` now contains:

- the shared auth pages for sign-in, callback, magic auth, email verification, MFA, organization selection, password recovery, and terminal auth states
- the shared `_AccessAuthLayout`
- the package-owned auth CSS and JavaScript
- the auth flow router and protected redirect/challenge state store
- unavailable-provider fallback services
- helper endpoints exposed by `MapAccessAuthenticationUiEndpoints()`

`Incursa.Integrations.WorkOS.AspNetCore` now contains:

- `AddWorkOsCustomUiAuthentication(...)` for provider registration
- a WorkOS-backed `IAccessPasswordRecoveryService`

`requestrelay` now consumes the shared auth UI and WorkOS adapter through local project references instead of shipping app-owned auth pages and a vendored WorkOS auth browser client.

## What Stays In The Host

The host app still owns:

- product-specific branding choices
- deployment-specific provider configuration
- authentication scheme naming
- app-specific authorization policy
- app-specific post-auth landing rules
- app-specific local organization selection if it is based on product data rather than provider-only membership data

That last point matters for `requestrelay`: the remaining `/signup` page is a local relay organization chooser. It is not provider auth UI and should stay host-owned unless multiple apps prove that exact post-auth step is reusable.

## Route Model

The package ships with built-in `/auth/*` page routes:

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

The package also maps helper endpoints:

- `/auth/login`
- `/auth/logout`
- `/auth/sign-out`

`AccessAuthenticationUiOptions.Routes` now does two things:

1. controls the helper endpoint targets and in-package navigation helpers
2. adds alternate Razor Page aliases when a host wants different auth paths

The built-in `/auth/*` routes still remain valid defaults.

## Provider Abstraction Boundary

The reusable UI package should not know about WorkOS-specific HTTP endpoints, request/response DTOs, or environment variables.

The correct boundary is:

- shared UI package depends on access contracts and optional provider-neutral seams
- provider package implements those seams and hides provider protocol details
- host app chooses the provider package and configures branding

That keeps swapping providers realistic. A host should change the provider adapter registration, not rewrite the sign-in pages.

## RequestRelay Consumption Pattern

The current local development shape is a pair of `ProjectReference`s from requestrelay into `platform`:

- `Incursa.Platform.Access.Razor`
- `Incursa.Integrations.WorkOS.AspNetCore`

That keeps the package editable in-place while the extraction settles. After publishing, those references can move to `PackageReference`s without changing the host wiring pattern.

## Publish Readiness

To publish this package set safely:

1. Build the shared package and provider adapter.
2. Run the focused auth tests.
3. Pack the Razor UI package and the WorkOS ASP.NET Core adapter locally.
4. Keep `eng/package-versions.json` and the affected packable project versions in sync.
5. Verify a consuming host still works through local project references or local packages before switching to a registry feed.

Recommended validation commands:

```powershell
dotnet build src/Incursa.Platform.Access.Razor/Incursa.Platform.Access.Razor.csproj -c Debug
dotnet build src/Incursa.Integrations.WorkOS.AspNetCore/Incursa.Integrations.WorkOS.AspNetCore.csproj -c Debug
dotnet test tests/Incursa.Platform.Access.AspNetCore.Tests/Incursa.Platform.Access.AspNetCore.Tests.csproj -c Debug --no-build
dotnet pack src/Incursa.Platform.Access.Razor/Incursa.Platform.Access.Razor.csproj -c Debug
dotnet pack src/Incursa.Integrations.WorkOS.AspNetCore/Incursa.Integrations.WorkOS.AspNetCore.csproj -c Debug
dotnet build ../requestrelay/RequestRelay.slnx -c Debug
dotnet test ../requestrelay/tests/Incursa.RequestRelay.Tests/Incursa.RequestRelay.Tests.csproj -c Debug --no-build --filter RelayUiAuthenticationPageModelTests
```

## Next Reuse Step

The next meaningful milestone is not more UI polish. It is a second consuming host.

Once a second app adopts `Incursa.Platform.Access.Razor`, we can validate which parts of post-auth onboarding are truly common and whether another provider needs additional neutral seams. Until then, the current package boundary is the pragmatic one: shared auth UI in `platform`, provider specifics in adapter packages, and app-specific onboarding left in the host.
