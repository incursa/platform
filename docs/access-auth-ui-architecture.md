# Access authentication UI architecture

## Decision

Reusable browser authentication UI lives in `Incursa.Platform.Access.Razor`. The package is provider-neutral and sits beside the access contracts and ASP.NET Core hosting helpers. Provider packages plug into it through `IAccessAuthenticationService`, `IAccessAuthenticationTicketService`, and the optional `IAccessPasswordRecoveryService` seam.

## Layering

`Incursa.Platform.Access` owns access contracts, challenge and result models, and authenticated session models.

`Incursa.Platform.Access.AspNetCore` owns cookie-backed local session persistence, ticket issuance and sign-out helpers, and request access-context integration.

`Incursa.Platform.Access.Razor` owns:

- provider-neutral authentication Razor Pages and shell assets;
- redirect state and pending challenge persistence;
- provider presentation, route, and branding options;
- login, logout, callback, verification, MFA, organization-selection, recovery, and terminal-state pages; and
- helper endpoints for login, logout, and sign-out.

Provider adapters own protocol calls, provider-specific configuration, recovery adapters, and host registration helpers.

## Host boundary

The consuming application owns product branding choices, deployment configuration, authentication scheme naming, authorization policy, post-auth landing rules, and onboarding or organization selection based on product data.

Shared provider authentication belongs in the reusable package. Product workflow and product data remain host-owned.

## Routes

The package supplies built-in `/auth/*` page routes for sign-in, callback, magic authentication, email verification, MFA, organization selection, password recovery, errors, access denial, logout, and session expiration. It also maps login, logout, and sign-out helper endpoints.

`AccessAuthenticationUiOptions.Routes` controls helper targets and package navigation and may add alternate Razor Page aliases. The built-in routes remain valid defaults.

## Provider abstraction

The UI package must not depend on provider-specific endpoints, DTOs, credentials, or environment variables. Hosts swap providers by changing adapter registration, not by rewriting shared pages.

## Validation and release

Build, test, and pack the shared package and each provider adapter independently. Verify a representative host through declared package or explicit local project references without documenting machine-specific paths. Registry publication, provider configuration, secrets, and deployment remain external controls and require verification in their owning systems.
