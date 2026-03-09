# Incursa Platform

`Incursa.Platform` is the public monorepo for Incursa infrastructure and platform libraries. It holds reusable cross-cutting capabilities, storage providers, hosting adapters, and selected public service integrations that can be explained without private product context.

## Monorepo shape

- `src/`: public packages that are allowed to ship from this repository
- `tests/`: automated tests plus smoke/sample apps
- `tools/`: analyzers and build/runtime helpers that are intentionally shipped
- `eng/`: package catalog, affected-project resolution, and pack/release helpers
- `docs/`: architecture, guidance, quality rules, and reference docs
- `incubating/`: preserved imports and staging code that remain non-packable and non-publishable by default

Authoritative governance lives in:

- `eng/package-catalog.json`
- `docs/architecture/monorepo.md`
- `docs/architecture/imported-integrations.md`
- `docs/quality/repo-scope-boundary.md`
- `incubating/README.md`

## Package families

Core and foundations:

- `Incursa.Platform`

Reusable capabilities:

- `Incursa.Platform.Audit`
- `Incursa.Platform.Correlation`
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

Providers and data adapters:

- `Incursa.Platform.SqlServer`
- `Incursa.Platform.Postgres`
- `Incursa.Platform.InMemory`
- `Incursa.Platform.Email.SqlServer`
- `Incursa.Platform.Email.Postgres`
- `Incursa.Integrations.Storage.Azure`

Public service integrations:

- `Incursa.Platform.Audit.WorkOS`
- `Incursa.Platform.Email.Postmark`

Hosting adapters:

- `Incursa.Platform.Email.AspNetCore`
- `Incursa.Platform.Health.AspNetCore`
- `Incursa.Platform.Metrics.AspNetCore`
- `Incursa.Platform.Metrics.HttpServer`
- `Incursa.Platform.Modularity.AspNetCore`
- `Incursa.Platform.Modularity.Razor`
- `Incursa.Platform.Webhooks.AspNetCore`

Tools:

- `Incursa.Platform.Observability.Analyzers`
- `Incursa.TestDocs.Analyzers`
- `Incursa.TestDocs.Cli`
- `Incursa.Platform.SchemaMigrations.Cli`

## Imported integrations

The monorepo now absorbs the public email/Postmark family and preserves broader vendor imports under `incubating/` until they have clear package boundaries.

- `C:\src\incursa\integrations-postmark` merged into `src/Incursa.Platform.Email*` plus `tests/Incursa.Platform.Email.Tests`
- `C:\src\incursa\integrations-workos` preserved under `incubating/workos/`; the existing public `Incursa.Platform.Audit.WorkOS` package remains the supported public WorkOS slice
- `C:\src\incursa\integrations-cloudflare` preserved under `incubating/cloudflare/`
- `C:\src\incursa\integrations-electronicnotary` preserved under `incubating/electronicnotary/`

## Solutions

- `Incursa.Platform.slnx`: primary working solution for public packages, tests, tools, and monorepo docs
- `Incursa.Platform.CI.slnx`: CI/build solution for public source, tests, smoke apps, and shipped tools
- `Incursa.Platform.Incubating.slnx`: preserved staging solution for imported code that is not part of the public release surface

## Pack and release policy

Projects do not become NuGet packages by accident.

- `Directory.Build.props` defaults all `.csproj` projects to `IsPackable=false`
- `Directory.Build.targets` defaults all builds to `GeneratePackageOnBuild=false`
- public packages opt back in explicitly with `IsPackable=true`
- `incubating/` projects are forced non-packable/non-publishable
- `eng/package-catalog.json` is the allowlist for pack/publish behavior
- `eng/Pack-PublicPackages.ps1` packs only the catalog-selected projects
- commit CI packs only affected public packages
- main/release CI can pack publishable packages, but nuget.org publishing is manual via workflow dispatch

## Local validation

```powershell
dotnet restore Incursa.Platform.CI.slnx
dotnet tool restore
dotnet build Incursa.Platform.CI.slnx -c Release
dotnet test Incursa.Platform.CI.slnx -c Release
pwsh -File eng/Generate-PackageCatalog.ps1
pwsh -File eng/Pack-PublicPackages.ps1 -Configuration Release -OutputPath ./nupkgs
```

Repo-specific orientation and curated references live in `llms.txt`.
