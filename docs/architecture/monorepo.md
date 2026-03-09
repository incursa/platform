# Monorepo Architecture

## Intent

`Incursa.Platform` is a public infrastructure monorepo. The repository should feel like one coherent family of platform libraries, not a catch-all for unrelated code. Public packages must be explainable without private business, product, tenant, or workflow context.

## Zones

### `src/`

Public packages that are allowed to ship from this repository.

Families:

- `core`: foundational primitives and abstractions
- `capabilities`: reusable cross-cutting building blocks
- `providers`: storage/database/provider implementations
- `integrations`: capability-specific public vendor adapters; when the package is vendor-owned rather than capability-owned, prefer the `Incursa.Integrations.*` naming family
- `hosting`: ASP.NET Core and host integration glue

### `tests/`

Automated tests plus smoke/sample applications used to validate the public surface.

### `tools/`

Shipped analyzers and helper CLIs that support the package family.

### `eng/`

Monorepo governance and release automation:

- `eng/package-catalog.json`
- `eng/Generate-PackageCatalog.ps1`
- `eng/Resolve-AffectedProjects.ps1`
- `eng/Pack-PublicPackages.ps1`

### `incubating/`

Preserved code that may still be useful but is not part of the public package surface yet. Incubating code should remain buildable when practical, but it is non-packable and non-publishable by default.

## Current public families

Core:

- `Incursa.Platform`

Capabilities:

- `Incursa.Platform.Access`
- `Incursa.Platform.Audit`
- `Incursa.Platform.Correlation`
- `Incursa.Platform.CustomDomains`
- `Incursa.Platform.Dns`
- `Incursa.Platform.Email`
- `Incursa.Platform.ExactlyOnce`
- `Incursa.Platform.Health`
- `Incursa.Platform.HealthProbe`
- `Incursa.Platform.Idempotency`
- `Incursa.Platform.Modularity`
- `Incursa.Platform.Observability`
- `Incursa.Platform.Operations`
- `Incursa.Platform.Storage`
- `Incursa.Platform.Webhooks`

Providers:

- `Incursa.Platform.SqlServer`
- `Incursa.Platform.Postgres`
- `Incursa.Platform.InMemory`
- `Incursa.Platform.Email.SqlServer`
- `Incursa.Platform.Email.Postgres`
- `Incursa.Integrations.Storage.Azure`

Integrations:

- `Incursa.Integrations.WorkOS`
- `Incursa.Integrations.WorkOS.Abstractions`
- `Incursa.Integrations.WorkOS.Access`
- `Incursa.Integrations.WorkOS.AspNetCore`
- `Incursa.Integrations.WorkOS.Audit`
- `Incursa.Integrations.WorkOS.Webhooks`
- `Incursa.Integrations.Cloudflare`
- `Incursa.Integrations.Cloudflare.CustomDomains`
- `Incursa.Integrations.Cloudflare.Dns`
- `Incursa.Integrations.ElectronicNotary`
- `Incursa.Integrations.ElectronicNotary.Abstractions`
- `Incursa.Integrations.ElectronicNotary.Proof`
- `Incursa.Integrations.ElectronicNotary.Proof.AspNetCore`
- `Incursa.Platform.Email.Postmark`

Hosting:

- `Incursa.Platform.Access.AspNetCore`
- `Incursa.Platform.Email.AspNetCore`
- `Incursa.Platform.Health.AspNetCore`
- `Incursa.Platform.Metrics.AspNetCore`
- `Incursa.Platform.Metrics.HttpServer`
- `Incursa.Platform.Modularity.AspNetCore`
- `Incursa.Platform.Modularity.Razor`
- `Incursa.Platform.Webhooks.AspNetCore`

## Solutions

- `Incursa.Platform.slnx` groups the public monorepo by package family and includes docs/governance files.
- `Incursa.Platform.CI.slnx` is the build/test solution for public packages, tests, smoke apps, and shipped tools.
- `Incursa.Platform.Incubating.slnx` is reserved for future imported staging code outside the default public CI path.

## Layer 2 capability families

The repository now includes explicit layer 2 capability packages that separate provider-neutral domain surfaces from provider-specific adapters:

- access: `Incursa.Platform.Access` with `Incursa.Integrations.WorkOS.Access`
- custom domains: `Incursa.Platform.CustomDomains` with `Incursa.Integrations.Cloudflare.CustomDomains`
- DNS: `Incursa.Platform.Dns` with `Incursa.Integrations.Cloudflare.Dns`

These packages keep local source-of-truth models in the capability library and isolate provider materialization in focused adapters. Broader vendor-specific functionality can still ship as `Incursa.Integrations.*` packages without becoming layer 2 capabilities. See `docs/architecture/layer-2-capabilities.md`.

Public layer 1 vendor packages are not considered incubating simply because they are vendor-specific. If a vendor adapter has a clean boundary, tests, and release metadata, it belongs in `src/` as a packable public integration package. `incubating/` is reserved for staging code that is still too broad, too workflow-heavy, or otherwise not ready for the public package surface.

## Pack and publish policy

The repository defaults to a safe, explicit packaging model:

- `Directory.Build.props` defaults `.csproj` projects to `IsPackable=false`
- `Directory.Build.targets` defaults all builds to `GeneratePackageOnBuild=false`
- public packages and shipped tools opt back in with `IsPackable=true`
- `eng/package-catalog.json` is the authoritative pack/publish allowlist
- `eng/Pack-PublicPackages.ps1` is the supported packing entrypoint
- commit CI packs affected public packages only
- main/release CI packs publishable packages, but public publishing is manual

## Promotion rule

Code moves from `incubating/` into `src/` only when:

- the capability boundary is clear
- package naming is coherent
- the package can stand on its own as public infrastructure
- tests/docs are good enough for the public surface

If those conditions are not met, keep the code preserved in `incubating/`.
