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
- `integrations`: capability-specific public service adapters
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

- `Incursa.Platform.Access.WorkOS`
- `Incursa.Platform.Audit.WorkOS`
- `Incursa.Platform.CustomDomains.Cloudflare`
- `Incursa.Platform.Dns.Cloudflare`
- `Incursa.Platform.Email.Postmark`

Hosting:

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
- `Incursa.Platform.Incubating.slnx` preserves imported staging code outside the default public CI path.

## Layer 2 capability families

The repository now includes explicit layer 2 capability packages that separate provider-neutral domain surfaces from provider-specific adapters:

- access: `Incursa.Platform.Access` with `Incursa.Platform.Access.WorkOS`
- custom domains: `Incursa.Platform.CustomDomains` with `Incursa.Platform.CustomDomains.Cloudflare`
- DNS: `Incursa.Platform.Dns` with `Incursa.Platform.Dns.Cloudflare`

These packages keep local source-of-truth models in the capability library and isolate provider materialization in focused adapters. Broader vendor buckets remain preserved in `incubating/` until they can be split on clean public boundaries. The remaining WorkOS code is intentionally deferred until it can be expressed as smaller access- or webhook-oriented adapters instead of a second identity core. See `docs/architecture/layer-2-capabilities.md`.

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
