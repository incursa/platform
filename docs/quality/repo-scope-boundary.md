# Platform Scope Boundary

`platform` is intentionally scoped to provider-neutral code. Keep this repository focused on abstractions, shared models, orchestration, hosting adapters, and tooling that can be reused without taking a dependency on a concrete provider implementation.

## In scope

- durable-processing primitives and orchestration
- reusable cross-cutting capabilities such as access, audit, correlation, custom domains, DNS, email, health, modularity, operations, storage, and webhooks
- hosting adapters and repo-agnostic ASP.NET Core glue
- shared analyzers, helper CLIs, and provider-neutral test utilities
- staging code in `incubating/` when it is not yet ready for the public package surface

## Out of scope

- concrete vendor API adapters
- public storage and database implementation packages
- provider-specific tests and smoke hosts whose main purpose is validating moved implementations
- customer-specific or proprietary integrations

## Placement rules

- if the code can be explained without naming a provider, it probably belongs here
- if the code exists to translate to or from a specific provider or backing store, move it to `integrations-public` or `integrations-private`
- if classification is unclear, keep the shared abstraction here and document the unresolved boundary in the split notes

## Release guardrails

- all projects default to `IsPackable=false` and `GeneratePackageOnBuild=false`
- only catalog-allowlisted projects in `eng/package-catalog.json` can be packed or published
- `incubating/` remains non-packable and non-publishable by default
