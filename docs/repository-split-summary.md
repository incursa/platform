# Repository Split Summary

## What remained in `platform`

Source projects:

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

Tests and tooling:

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
- `tools/observability/src/Incursa.Platform.Observability.Analyzers/`
- `tools/testdocs/src/Incursa.TestDocs.Analyzers/`
- `tools/testdocs/src/Incursa.TestDocs.Cli/`

## What moved to `integrations-public`

- `Incursa.Integrations.Cloudflare*`
- `Incursa.Integrations.ElectronicNotary*`
- `Incursa.Integrations.Storage.Azure`
- `Incursa.Integrations.Stripe`
- `Incursa.Integrations.WorkOS*`
- `Incursa.Platform.Email.Postmark`
- `Incursa.Platform.Email.Postgres`
- `Incursa.Platform.Email.SqlServer`
- `Incursa.Platform.InMemory`
- `Incursa.Platform.Postgres`
- `Incursa.Platform.SqlServer`
- the tests and smoke hosts that primarily validate those implementations
- `tools/migrations/src/Incursa.Platform.SchemaMigrations.Cli/`

## Current dependency rule

- `platform` is the stable base
- `integrations-public` can depend on `platform`
- `platform` must not depend on `integrations-public`

## Unresolved follow-up work

- replace sibling project references with package-based local development once publishing workflows are in place
- continue evaluating whether any remaining mixed documentation should be split further as the repos stabilize
- populate `integrations-private` when proprietary providers are isolated enough to move safely
