# Repository Architecture

## Intent

`platform` is the provider-neutral base repository in the Incursa split. It should hold reusable abstractions, shared models, orchestration, hosting adapters, and tooling that can be consumed by public or private integration repositories.

## Zones

### `src/`

Provider-neutral packages that remain in this repository.

Families currently retained here:

- `Incursa.Platform`
- `Incursa.Platform.Access`
- `Incursa.Platform.Access.AspNetCore`
- `Incursa.Platform.Access.Razor`
- `Incursa.Platform.Audit`
- `Incursa.Platform.Correlation`
- `Incursa.Platform.CustomDomains`
- `Incursa.Platform.Dns`
- `Incursa.Platform.Email`
- `Incursa.Platform.Email.AspNetCore`
- `Incursa.Platform.ExactlyOnce`
- `Incursa.Platform.Health`
- `Incursa.Platform.Health.AspNetCore`
- `Incursa.Platform.HealthProbe`
- `Incursa.Platform.Idempotency`
- `Incursa.Platform.Metrics.AspNetCore`
- `Incursa.Platform.Metrics.HttpServer`
- `Incursa.Platform.Modularity`
- `Incursa.Platform.Modularity.AspNetCore`
- `Incursa.Platform.Modularity.Razor`
- `Incursa.Platform.Observability`
- `Incursa.Platform.Operations`
- `Incursa.Platform.Storage`
- `Incursa.Platform.Webhooks`
- `Incursa.Platform.Webhooks.AspNetCore`

### `tests/`

Provider-neutral tests and shared test utilities. Tests that primarily validate moved provider implementations were extracted to `integrations-public`.

### `tools/`

Shared analyzers and helper CLIs that are not specific to a provider implementation.

### `eng/`

Package catalog, versioning, and affected-project automation for the retained platform surface.

### `docs/`

Architecture notes, scope rules, and repository split records.

### `incubating/`

Staging area for code that is not yet ready for the public package surface. It is not the default home for active provider implementations.

## Solutions

- `Incursa.Platform.slnx` is the day-to-day solution for the retained platform surface.
- `Incursa.Platform.CI.slnx` is the build/test/pack solution for this repo.
- `Incursa.Platform.Incubating.slnx` remains available for staging work that stays outside the default CI path.

## Dependency boundary

- `platform` must not depend on `integrations-public`
- `integrations-public` and future `integrations-private` repos may depend on `platform`
- concrete provider implementations should be extracted out instead of being re-added here

## Packaging policy

- keep this repo focused on provider-neutral packages and shared tooling
- packability remains opt-in per project
- `eng/package-catalog.json` is the authoritative allowlist for this repo
- `eng/package-versions.json` is the authoritative per-package version manifest for packable projects
