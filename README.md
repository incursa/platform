# Incursa Platform

`Incursa.Platform` is a public .NET monorepo for reusable platform capabilities, hosting adapters, and vendor integrations. The repository is organized around two package layers:

- Layer 2 capability packages in `src/Incursa.Platform.*`
- Layer 1 vendor integration packages in `src/Incursa.Integrations.*`

The goal is to keep provider-neutral contracts and domain models clean, small, and reusable, while still shipping first-class vendor-specific adapters for the systems those capabilities need to talk to.

## Start Here

- [Monorepo architecture](docs/architecture/monorepo.md)
- [Layer 2 capability model](docs/architecture/layer-2-capabilities.md)
- [Imported integrations and provider boundaries](docs/architecture/imported-integrations.md)
- [Repository scope and public-surface rules](docs/quality/repo-scope-boundary.md)
- [Curated repository map for contributors and LLMs](llms.txt)

## Repository Layout

- [`src/`](src/) contains public packages that are intended to ship from this repository.
- [`tests/`](tests/) contains automated tests and sample or smoke-host projects.
- [`docs/`](docs/) contains architecture notes, quality rules, runbooks, and reference material.
- [`eng/`](eng/) contains packaging, affected-project, and release support scripts.
- [`tools/`](tools/) contains shipped build/runtime helpers and test-support utilities.
- [`incubating/`](incubating/) is a staging area reserved for code that is not yet ready for the public source tree.

## Layer 2 Capabilities

These packages define the reusable, provider-neutral capability surface of the platform.

### Core and foundation

- [`Incursa.Platform`](src/Incursa.Platform/) for foundational infrastructure, orchestration, and cross-cutting runtime building blocks
- [`Incursa.Platform.Storage`](src/Incursa.Platform.Storage/) for partition-aware storage contracts used by capability and adapter packages
- [`Incursa.Platform.Observability`](src/Incursa.Platform.Observability/) for shared observability conventions and event/tag naming
- [`Incursa.Platform.Correlation`](src/Incursa.Platform.Correlation/) for correlation context propagation
- [`Incursa.Platform.Audit`](src/Incursa.Platform.Audit/) for immutable audit-event contracts
- [`Incursa.Platform.Operations`](src/Incursa.Platform.Operations/) for long-running operation tracking primitives

### Business and integration capabilities

- [`Incursa.Platform.Access`](src/Incursa.Platform.Access/) for users, scope roots, tenants, memberships, assignments, grants, and effective-access evaluation
- [`Incursa.Platform.Dns`](src/Incursa.Platform.Dns/) for provider-neutral zone and record management
- [`Incursa.Platform.CustomDomains`](src/Incursa.Platform.CustomDomains/) for managed custom-domain and custom-hostname lifecycle state
- [`Incursa.Platform.Email`](src/Incursa.Platform.Email/) for outbound email contracts, queueing, dispatch, and delivery workflow primitives
- [`Incursa.Platform.Webhooks`](src/Incursa.Platform.Webhooks/) for provider-agnostic webhook ingestion and classification primitives
- [`Incursa.Platform.Health`](src/Incursa.Platform.Health/) for service and subsystem health surfaces
- [`Incursa.Platform.Modularity`](src/Incursa.Platform.Modularity/) for module registration and engine composition

### Hosting and provider-neutral adapters

- [`Incursa.Platform.Access.AspNetCore`](src/Incursa.Platform.Access.AspNetCore/)
- [`Incursa.Platform.Email.AspNetCore`](src/Incursa.Platform.Email.AspNetCore/)
- [`Incursa.Platform.Health.AspNetCore`](src/Incursa.Platform.Health.AspNetCore/)
- [`Incursa.Platform.Metrics.AspNetCore`](src/Incursa.Platform.Metrics.AspNetCore/)
- [`Incursa.Platform.Metrics.HttpServer`](src/Incursa.Platform.Metrics.HttpServer/)
- [`Incursa.Platform.Modularity.AspNetCore`](src/Incursa.Platform.Modularity.AspNetCore/)
- [`Incursa.Platform.Modularity.Razor`](src/Incursa.Platform.Modularity.Razor/)
- [`Incursa.Platform.Webhooks.AspNetCore`](src/Incursa.Platform.Webhooks.AspNetCore/)

### Storage and persistence adapters

- [`Incursa.Platform.InMemory`](src/Incursa.Platform.InMemory/)
- [`Incursa.Platform.Postgres`](src/Incursa.Platform.Postgres/)
- [`Incursa.Platform.SqlServer`](src/Incursa.Platform.SqlServer/)
- [`Incursa.Platform.Email.Postgres`](src/Incursa.Platform.Email.Postgres/)
- [`Incursa.Platform.Email.SqlServer`](src/Incursa.Platform.Email.SqlServer/)
- [`Incursa.Platform.Email.Postmark`](src/Incursa.Platform.Email.Postmark/)

## Layer 1 Integrations

These packages are vendor-specific adapters. They are public, packable packages, but they are intentionally scoped to a particular provider.

### WorkOS

- [`Incursa.Integrations.WorkOS`](src/Incursa.Integrations.WorkOS/)
- [`Incursa.Integrations.WorkOS.Abstractions`](src/Incursa.Integrations.WorkOS.Abstractions/)
- [`Incursa.Integrations.WorkOS.Access`](src/Incursa.Integrations.WorkOS.Access/)
- [`Incursa.Integrations.WorkOS.AspNetCore`](src/Incursa.Integrations.WorkOS.AspNetCore/)
- [`Incursa.Integrations.WorkOS.Audit`](src/Incursa.Integrations.WorkOS.Audit/)
- [`Incursa.Integrations.WorkOS.Webhooks`](src/Incursa.Integrations.WorkOS.Webhooks/)
- [`Incursa.Integrations.WorkOS.AppAuth.Abstractions`](src/Incursa.Integrations.WorkOS.AppAuth.Abstractions/)
- [`Incursa.Integrations.WorkOS.AppAuth.AspNetCore`](src/Incursa.Integrations.WorkOS.AppAuth.AspNetCore/)

### Cloudflare

- [`Incursa.Integrations.Cloudflare`](src/Incursa.Integrations.Cloudflare/)
- [`Incursa.Integrations.Cloudflare.Dns`](src/Incursa.Integrations.Cloudflare.Dns/)
- [`Incursa.Integrations.Cloudflare.CustomDomains`](src/Incursa.Integrations.Cloudflare.CustomDomains/)
- [`Incursa.Integrations.Cloudflare.KvProbe`](src/Incursa.Integrations.Cloudflare.KvProbe/)

### Electronic notary

- [`Incursa.Integrations.ElectronicNotary`](src/Incursa.Integrations.ElectronicNotary/)
- [`Incursa.Integrations.ElectronicNotary.Abstractions`](src/Incursa.Integrations.ElectronicNotary.Abstractions/)
- [`Incursa.Integrations.ElectronicNotary.Proof`](src/Incursa.Integrations.ElectronicNotary.Proof/)
- [`Incursa.Integrations.ElectronicNotary.Proof.AspNetCore`](src/Incursa.Integrations.ElectronicNotary.Proof.AspNetCore/)

### Storage providers

- [`Incursa.Integrations.Storage.Azure`](src/Incursa.Integrations.Storage.Azure/)

## How To Navigate The Repo

- Start with a layer 2 package if you are trying to understand the platform’s public domain model or capability boundary.
- Start with a layer 1 integration package if you already know the provider you need and want the vendor-specific adapter.
- Use the architecture docs before moving code between package families; those documents define what belongs in capability packages versus provider packages.

## Packaging And Release Model

- Public packages live under `src/`.
- Packability is explicitly opted into per project.
- [`eng/package-catalog.json`](eng/package-catalog.json) is the allowlist used by repo tooling for package discovery and packing.
- [`eng/Generate-PackageCatalog.ps1`](eng/Generate-PackageCatalog.ps1) refreshes the package catalog.
- [`eng/Pack-PublicPackages.ps1`](eng/Pack-PublicPackages.ps1) packs the public package set.

## Local Validation

```powershell
dotnet restore Incursa.Platform.CI.slnx
dotnet tool restore
dotnet build Incursa.Platform.CI.slnx -c Release
dotnet test Incursa.Platform.CI.slnx -c Release
pwsh -File eng/Generate-PackageCatalog.ps1
pwsh -File eng/Pack-PublicPackages.ps1 -Configuration Release -OutputPath ./nupkgs
```
