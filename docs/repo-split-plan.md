# Repository Split Plan

## Intent

This repository is being split so that `platform` remains the provider-neutral source of truth and `integrations-public` carries public provider-specific implementations.

## Bucket A: Stay In `platform`

### Source projects

- `src/Incursa.Platform/`
- `src/Incursa.Platform.Access/`
- `src/Incursa.Platform.Access.AspNetCore/`
- `src/Incursa.Platform.Access.Razor/`
- `src/Incursa.Platform.Audit/`
- `src/Incursa.Platform.Correlation/`
- `src/Incursa.Platform.CustomDomains/`
- `src/Incursa.Platform.Dns/`
- `src/Incursa.Platform.Email/`
- `src/Incursa.Platform.Email.AspNetCore/`
- `src/Incursa.Platform.ExactlyOnce/`
- `src/Incursa.Platform.Health/`
- `src/Incursa.Platform.Health.AspNetCore/`
- `src/Incursa.Platform.HealthProbe/`
- `src/Incursa.Platform.Idempotency/`
- `src/Incursa.Platform.Metrics.AspNetCore/`
- `src/Incursa.Platform.Metrics.HttpServer/`
- `src/Incursa.Platform.Modularity/`
- `src/Incursa.Platform.Modularity.AspNetCore/`
- `src/Incursa.Platform.Modularity.Razor/`
- `src/Incursa.Platform.Observability/`
- `src/Incursa.Platform.Operations/`
- `src/Incursa.Platform.Storage/`
- `src/Incursa.Platform.Webhooks/`
- `src/Incursa.Platform.Webhooks.AspNetCore/`

Reason:
These packages are provider-neutral foundations, capability surfaces, or hosting adapters that other repositories can depend on without taking a vendor dependency.

### Test projects

- `tests/Incursa.Platform.Access.AspNetCore.Tests/`
- `tests/Incursa.Platform.Audit.Tests/`
- `tests/Incursa.Platform.Correlation.Tests/`
- `tests/Incursa.Platform.HealthProbe.Tests/`
- `tests/Incursa.Platform.Observability.Tests/`
- `tests/Incursa.Platform.Operations.Tests/`
- `tests/Incursa.Platform.Storage.Tests/`
- `tests/Incursa.Platform.Tests/`
- `tests/Incursa.Platform.TestUtilities/`
- `tests/Incursa.Platform.Webhooks.AspNetCore.Tests/`
- `tests/Incursa.Platform.Webhooks.Tests/`

Reason:
These test projects validate provider-neutral runtime and hosting behavior. `tests/Incursa.Platform.Tests/` keeps the core suite after the WorkOS-specific audit sink tests move out.

## Bucket B: Move To `integrations-public`

### Source projects

- `src/Incursa.Integrations.Cloudflare/`
- `src/Incursa.Integrations.Cloudflare.CustomDomains/`
- `src/Incursa.Integrations.Cloudflare.Dns/`
- `src/Incursa.Integrations.Cloudflare.KvProbe/`
- `src/Incursa.Integrations.ElectronicNotary/`
- `src/Incursa.Integrations.ElectronicNotary.Abstractions/`
- `src/Incursa.Integrations.ElectronicNotary.Proof/`
- `src/Incursa.Integrations.ElectronicNotary.Proof.AspNetCore/`
- `src/Incursa.Integrations.Storage.Azure/`
- `src/Incursa.Integrations.Stripe/`
- `src/Incursa.Integrations.WorkOS/`
- `src/Incursa.Integrations.WorkOS.Abstractions/`
- `src/Incursa.Integrations.WorkOS.Access/`
- `src/Incursa.Integrations.WorkOS.AspNetCore/`
- `src/Incursa.Integrations.WorkOS.Audit/`
- `src/Incursa.Integrations.WorkOS.Webhooks/`
- `src/Incursa.Platform.Email.Postmark/`
- `src/Incursa.Platform.Email.Postgres/`
- `src/Incursa.Platform.Email.SqlServer/`
- `src/Incursa.Platform.InMemory/`
- `src/Incursa.Platform.Postgres/`
- `src/Incursa.Platform.SqlServer/`

Reason:
These projects are concrete public integrations or provider implementations. They can depend on `platform`, but `platform` should not carry them after the split.

### Test and smoke projects

- `tests/Incursa.Integrations.Cloudflare.IntegrationTests/`
- `tests/Incursa.Integrations.Cloudflare.Tests/`
- `tests/Incursa.Integrations.ElectronicNotary.Tests/`
- `tests/Incursa.Integrations.Storage.Azure.Tests/`
- `tests/Incursa.Integrations.WorkOS.Tests/`
- `tests/Incursa.Integrations.WorkOS.Webhooks.Tests/`
- `tests/Incursa.Platform.Access.Tests/`
- `tests/Incursa.Platform.CustomDomains.Tests/`
- `tests/Incursa.Platform.Dns.Tests/`
- `tests/Incursa.Platform.Email.Tests/`
- `tests/Incursa.Platform.InMemory.Tests/`
- `tests/Incursa.Platform.Postgres.Tests/`
- `tests/Incursa.Platform.Smoke.AppHost/`
- `tests/Incursa.Platform.Smoke.ServiceDefaults/`
- `tests/Incursa.Platform.SmokeWeb/`
- `tests/Incursa.Platform.SqlServer.Tests/`

Reason:
These tests and smoke hosts directly validate moved provider implementations, or they already reference moved `Incursa.Integrations.*` packages.

## Bucket C: Unclear Or Deferred

- `incubating/`
- `Incursa.Platform.Incubating.slnx`
- provider-traceability and quality lane scripts that still use `Platform` naming

Reason:
These assets are not primary build inputs for the split itself, but they still reflect the old monorepo history and naming. They should be narrowed further in a follow-up once the two active repos are stable.

## Assumptions

- `docs/architecture/imported-integrations.md` and `eng/package-catalog.json` are authoritative enough to classify current provider-specific packages.
- Public storage/database implementations belong with `integrations-public` even when their package names remain `Incursa.Platform.*`.
- Cross-repository `ProjectReference` links are acceptable as the first local-development bridge while packaging workflows catch up.
