# Access Auth UI Architecture

## Decision

The reusable authentication experience should live in a provider-neutral Razor class library inside `platform`, not inside a WorkOS-specific integration package and not as a copy-paste pattern that every app forks.

The recommended package boundary is:

- `Incursa.Platform.Access.Razor`

That package should sit alongside the existing platform access packages and consume the already-provider-neutral `IAccessAuthenticationService` contracts from `Incursa.Platform.Access`.

## Why This Is The Best First Reuse Model

The requestrelay implementation proved two things at the same time:

- the auth screens are worth reusing
- the provider-specific logic is much thinner than the UI and flow-state logic

For the current codebase, the lowest-friction reusable shape is a Razor class library because it preserves the main advantages the app already has:

- pages can be copied into the package with minimal conceptual translation
- hosts can still override colors, copy, and route conventions
- the UI stays close to ASP.NET Core and Razor Pages rather than introducing another rendering abstraction too early
- WorkOS stays an adapter, not the defining shape of the UI

That is a better fit than trying to invent a brand-new engine or widget system for login before we have a second real provider to validate the abstraction.

## Layering

Use the layers below and keep them strict.

### Provider-neutral capability

`Incursa.Platform.Access`

Owns:

- app-facing authentication contracts
- challenge/result modeling
- authenticated session representation

### ASP.NET Core hosting

`Incursa.Platform.Access.AspNetCore`

Owns:

- cookie-backed local session persistence
- ticket issuance and sign-out helpers
- current request access-context resolution

### Reusable auth UI

`Incursa.Platform.Access.Razor`

Owns:

- Razor Pages, layouts, and static assets for auth flows
- redirect/challenge state persistence for multi-step auth UI
- provider-button presentation and theme hooks
- route conventions and branding options
- optional password-recovery abstraction for providers that support it

### Provider adapters

Examples:

- `Incursa.Integrations.WorkOS.Access`
- `Incursa.Integrations.WorkOS.AspNetCore`

Own:

- provider protocol calls
- provider-specific middleware/endpoints
- provider-specific recovery implementations
- provider-specific host wiring

## What Moves Out Of RequestRelay

The following are extraction candidates for `Incursa.Platform.Access.Razor`:

- auth layouts and static assets
- sign-in, magic auth, email verification, MFA, organization selection, logged out, session expired, and error screens
- redirect-state and pending-challenge persistence
- auth flow routing after `IAccessAuthenticationService` calls
- provider button presentation metadata

The following should stay outside the provider-neutral UI package:

- direct WorkOS password reset client usage
- WorkOS environment/configuration models
- app-specific landing routes like `/relay`
- app-specific copy that is not generally reusable

## Password Recovery Strategy

Password recovery is the only major flow in the current requestrelay implementation that is not naturally provider-neutral today.

Do not put WorkOS password reset contracts directly into the reusable UI package.

Instead:

1. define an optional provider-neutral recovery seam in `Incursa.Platform.Access.Razor`
2. let WorkOS implement that seam in its adapter layer
3. allow apps to disable recovery screens entirely when no recovery service is registered

That keeps the reusable UI package honest about what is generic and what is provider-specific.

## Near-term Migration Plan

1. land the provider-neutral auth UI package with shared flow/state services and options
2. move the requestrelay auth pages and CSS/JS into the package incrementally
3. switch requestrelay from app-owned auth pages to package-owned pages with only branding/provider configuration
4. add a WorkOS-backed implementation of the optional recovery seam
5. validate the boundary by adding a second provider or a local membership-backed adapter later

## Host Customization Model

Hosts should be able to customize without forking the package:

- branding text via options
- provider button catalog via options
- CSS overrides through package-owned CSS variables and host stylesheets
- route placement through route options when needed

If a host needs deeper layout changes later, that should happen through Razor override points or partial replacement, not by duplicating the full flow implementation.
