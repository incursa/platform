# Platform Scope Boundary

This repository is the public monorepo for the `Incursa.Platform` family. It is intentionally scoped to reusable infrastructure/platform packages, shipped tooling, and a small set of public provider adapters that fit the capability model of the repo.

## Boundary decision

- `src/` is for public, explainable packages that can stand on their own without private app/business/domain context.
- `incubating/` is for preserved code that may still be valuable, but is not yet clean enough for the public release surface.
- `tests/` and `tools/` stay inside the monorepo when they support the public packages and release process.

## In scope for `src/`

- Core durable-processing primitives and abstractions.
- Reusable cross-cutting capabilities such as audit, operations, observability, idempotency, exactly-once, correlation, webhooks, modularity, storage, and email.
- Storage/database providers and provider adapters for those capabilities.
- Capability-specific integrations for widely used public services when the boundary is clear.
- Hosting adapters and ASP.NET Core integration packages.
- Shipped analyzers and CLIs that support the public package family.

## Move to `incubating/` when any of these are true

- The code mixes reusable infrastructure with product workflows, UX flows, policy logic, or tenant-specific conventions.
- The public package boundary is still unclear.
- The imported repository is a broad vendor bucket that should be split by capability before it ships.
- The code is useful to preserve, but not yet appropriate to publish from this monorepo.

## Current decisions

Public provider/service adapters allowed in `src/`:

- `Incursa.Platform.Audit.WorkOS`
- `Incursa.Platform.Email.Postmark`
- `Incursa.Integrations.Storage.Azure`

Preserved in `incubating/` until further split/cleanup:

- `incubating/cloudflare/`
- `incubating/workos/` outside the existing audit sink slice
- `incubating/electronicnotary/`

## Release guardrails

- All projects default to `IsPackable=false` and `GeneratePackageOnBuild=false`.
- Only catalog-allowlisted projects in `eng/package-catalog.json` can be packed or published.
- `incubating/` projects are non-packable and non-publishable by default.
- Commit CI packs only affected public packages.
- Main/release CI may pack publishable packages, but public publishing is manual.

## Scope enforcement

- Token-based guardrails live in `scripts/quality/platform-scope.rules.json`.
- Validation runs through `scripts/quality/validate-platform-scope.ps1`.
- Imported repo provenance and landing zones are documented in `docs/architecture/imported-integrations.md`.
