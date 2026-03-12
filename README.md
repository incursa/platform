# Incursa Platform

`platform` contains the provider-neutral foundation for the Incursa package family: abstractions, shared models, orchestration, hosting adapters, and tooling that public or private integration repositories can build on.

Public provider implementations have been split out to [integrations-public](C:/src/incursa/integrations-public). Future proprietary adapters are intended to live in [integrations-private](C:/src/incursa/integrations-private).

Dependency direction:

- `platform` must not depend on `integrations-public`
- `integrations-public` and `integrations-private` can depend on `platform`

## Start Here

- [Repository architecture](docs/architecture/monorepo.md)
- [Imported integration provenance](docs/architecture/imported-integrations.md)
- [Repository scope boundary](docs/quality/repo-scope-boundary.md)
- [Split plan](docs/repo-split-plan.md)
- [Split summary](docs/repository-split-summary.md)
- [Curated repo map](llms.txt)

## Repository Layout

- [`src/`](src/) contains provider-neutral packages and hosting adapters intended to stay in `platform`.
- [`tests/`](tests/) contains provider-neutral tests and shared test utilities that remain with this repo.
- [`docs/`](docs/) contains architecture notes, scope rules, and migration records.
- [`eng/`](eng/) contains package catalog, versioning, and release helpers.
- [`tools/`](tools/) contains shared analyzers and helper CLIs that remain provider-neutral.
- [`incubating/`](incubating/) is still reserved for staged code that is not yet part of the public package surface.

## Current Package Families

Core and capabilities:

- `Incursa.Platform`
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

Hosting adapters:

- `Incursa.Platform.Access.AspNetCore`
- `Incursa.Platform.Access.Razor`
- `Incursa.Platform.Email.AspNetCore`
- `Incursa.Platform.Health.AspNetCore`
- `Incursa.Platform.Metrics.AspNetCore`
- `Incursa.Platform.Metrics.HttpServer`
- `Incursa.Platform.Modularity.AspNetCore`
- `Incursa.Platform.Modularity.Razor`
- `Incursa.Platform.Webhooks.AspNetCore`

Shared tooling:

- `Incursa.Platform.Observability.Analyzers`
- `Incursa.TestDocs.Analyzers`
- `Incursa.TestDocs.Cli`

## Local Validation

```powershell
dotnet restore Incursa.Platform.CI.slnx
dotnet tool restore
dotnet build Incursa.Platform.CI.slnx -c Release
dotnet test Incursa.Platform.CI.slnx -c Release
pwsh -File eng/Generate-PackageCatalog.ps1
pwsh -File eng/Pack-PublicPackages.ps1 -Configuration Release -OutputPath ./nupkgs
```
