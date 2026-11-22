# Library Conformance Matrix

## Scope
Traceability matrix for cross-library interface quality scenarios in:
- `specs/libraries/library-interface-quality-spec.md`

Status values:
- `Covered`: scenario is mapped to one or more automated tests or automation artifacts.
- `Missing`: no mapped test exists yet.
- `Deferred`: intentionally deferred with rationale and backlog tracking.

| Scenario ID | Library | Area | Status | Mapped Test(s) / Artifact(s) |
| --- | --- | --- | --- | --- |
| LIB-GOV-SPEC-001 | All | Governance | Covered | `specs/libraries/library-interface-quality-spec.md`, `specs/libraries/library-conformance-matrix.md` |
| LIB-GOV-TEST-001 | All | Governance | Covered | `scripts/quality/validate-library-traceability.ps1` |
| LIB-GOV-COV-001 | All | Governance | Covered | `scripts/quality/run-library-coverage.ps1` |
| LIB-GOV-MUT-001 | All | Governance | Covered | `scripts/quality/run-library-mutation.ps1` |
| LIB-GOV-FUZZ-001 | All | Governance | Covered | `specs/libraries/library-conformance-matrix.md` |
| LIB-INMEMORY-API-001 | InMemory | PublicApi | Deferred | Moved to integrations-public; coverage is owned there |
| LIB-INMEMORY-TEST-001 | InMemory | Behavior | Deferred | Moved to integrations-public; coverage is owned there |
| LIB-INMEMORY-FUZZ-001 | InMemory | Fuzz | Deferred | Moved to integrations-public; coverage is owned there |
| LIB-INMEMORY-MUT-001 | InMemory | Mutation | Deferred | Moved to integrations-public; coverage is owned there |
| LIB-SQLSERVER-API-001 | SqlServer | PublicApi | Deferred | Moved to integrations-public; coverage is owned there |
| LIB-SQLSERVER-TEST-001 | SqlServer | Behavior | Deferred | Moved to integrations-public; coverage is owned there |
| LIB-SQLSERVER-FUZZ-001 | SqlServer | Fuzz | Deferred | Moved to integrations-public; coverage is owned there |
| LIB-SQLSERVER-MUT-001 | SqlServer | Mutation | Deferred | Moved to integrations-public; coverage is owned there |
| LIB-POSTGRES-API-001 | Postgres | PublicApi | Deferred | Moved to integrations-public; coverage is owned there |
| LIB-POSTGRES-TEST-001 | Postgres | Behavior | Deferred | Moved to integrations-public; coverage is owned there |
| LIB-POSTGRES-FUZZ-001 | Postgres | Fuzz | Deferred | Moved to integrations-public; coverage is owned there |
| LIB-POSTGRES-MUT-001 | Postgres | Mutation | Deferred | Moved to integrations-public; coverage is owned there |
| LIB-CORE-API-001 | Core | PublicApi | Covered | `tests/Incursa.Platform.Tests/OutboxMessageTests.cs` |
| LIB-CORE-TEST-001 | Core | Behavior | Covered | `tests/Incursa.Platform.Tests/Incursa.Platform.Tests.csproj` |
| LIB-AUDIT-API-001 | Audit | PublicApi | Covered | `tests/Incursa.Platform.Audit.Tests/AuditModelTests.cs` |
| LIB-AUDIT-TEST-001 | Audit | Behavior | Covered | `tests/Incursa.Platform.Audit.Tests/Incursa.Platform.Audit.Tests.csproj` |
| LIB-CORRELATION-API-001 | Correlation | PublicApi | Covered | `tests/Incursa.Platform.Correlation.Tests/CorrelationModelTests.cs` |
| LIB-CORRELATION-TEST-001 | Correlation | Behavior | Covered | `tests/Incursa.Platform.Correlation.Tests/Incursa.Platform.Correlation.Tests.csproj` |
| LIB-EMAIL-API-001 | Email | PublicApi | Missing | No direct Email public API tests yet |
| LIB-EMAIL-TEST-001 | Email | Behavior | Missing | No direct Email behavior suite yet |
| LIB-EMAILASPNETCORE-API-001 | Email.AspNetCore | PublicApi | Missing | No direct Email.AspNetCore public API tests yet |
| LIB-EMAILASPNETCORE-TEST-001 | Email.AspNetCore | Behavior | Missing | No direct Email.AspNetCore behavior suite yet |
| LIB-EXACTLYONCE-API-001 | ExactlyOnce | PublicApi | Covered | `tests/Incursa.Platform.Tests/ExactlyOnceExecutorTests.cs` |
| LIB-EXACTLYONCE-TEST-001 | ExactlyOnce | Behavior | Covered | `tests/Incursa.Platform.Tests/ExactlyOnceHandlerTests.cs` |
| LIB-HEALTHPROBE-API-001 | HealthProbe | PublicApi | Covered | `tests/Incursa.Platform.HealthProbe.Tests/HealthProbeCommandLineTests.cs` |
| LIB-HEALTHPROBE-TEST-001 | HealthProbe | Behavior | Covered | `tests/Incursa.Platform.HealthProbe.Tests/Incursa.Platform.HealthProbe.Tests.csproj` |
| LIB-IDEMPOTENCY-API-001 | Idempotency | PublicApi | Missing | No direct Idempotency library test project yet |
| LIB-IDEMPOTENCY-TEST-001 | Idempotency | Behavior | Missing | No direct Idempotency behavior test suite yet |
| LIB-METRICSASPNETCORE-API-001 | Metrics.AspNetCore | PublicApi | Missing | No direct Metrics.AspNetCore tests yet |
| LIB-METRICSASPNETCORE-TEST-001 | Metrics.AspNetCore | Behavior | Missing | No direct Metrics.AspNetCore behavior suite yet |
| LIB-METRICSHTTPSERVER-API-001 | Metrics.HttpServer | PublicApi | Missing | No direct Metrics.HttpServer tests yet |
| LIB-METRICSHTTPSERVER-TEST-001 | Metrics.HttpServer | Behavior | Missing | No direct Metrics.HttpServer behavior suite yet |
| LIB-MODULARITY-API-001 | Modularity | PublicApi | Covered | `tests/Incursa.Platform.Tests/Modularity/ModuleSystemTests.cs` |
| LIB-MODULARITY-TEST-001 | Modularity | Behavior | Covered | `tests/Incursa.Platform.Tests/Modularity/ModuleRegistryTestCollection.cs` |
| LIB-MODULARITYASPNETCORE-API-001 | Modularity.AspNetCore | PublicApi | Covered | `tests/Incursa.Platform.Tests/Modularity/RequiredServiceValidationTests.cs` |
| LIB-MODULARITYASPNETCORE-TEST-001 | Modularity.AspNetCore | Behavior | Covered | `tests/Incursa.Platform.Tests/Modularity/RequiredServiceValidationTests.cs` |
| LIB-MODULARITYRAZOR-API-001 | Modularity.Razor | PublicApi | Covered | `tests/Incursa.Platform.Tests/Modularity/RazorPagesConfigurationTests.cs` |
| LIB-MODULARITYRAZOR-TEST-001 | Modularity.Razor | Behavior | Covered | `tests/Incursa.Platform.Tests/Modularity/RazorPagesConfigurationTests.cs` |
| LIB-OBSERVABILITY-API-001 | Observability | PublicApi | Covered | `tests/Incursa.Platform.Observability.Tests/PlatformEventEmitterTests.cs` |
| LIB-OBSERVABILITY-TEST-001 | Observability | Behavior | Covered | `tests/Incursa.Platform.Observability.Tests/Incursa.Platform.Observability.Tests.csproj` |
| LIB-OPERATIONS-API-001 | Operations | PublicApi | Covered | `tests/Incursa.Platform.Operations.Tests/OperationModelTests.cs` |
| LIB-OPERATIONS-TEST-001 | Operations | Behavior | Covered | `tests/Incursa.Platform.Operations.Tests/Incursa.Platform.Operations.Tests.csproj` |
| LIB-WEBHOOKS-API-001 | Webhooks | PublicApi | Covered | `tests/Incursa.Platform.Webhooks.Tests/WebhookProcessorTests.cs` |
| LIB-WEBHOOKS-TEST-001 | Webhooks | Behavior | Covered | `tests/Incursa.Platform.Webhooks.Tests/Incursa.Platform.Webhooks.Tests.csproj` |
| LIB-WEBHOOKSASPNETCORE-API-001 | Webhooks.AspNetCore | PublicApi | Covered | `tests/Incursa.Platform.Webhooks.AspNetCore.Tests/WebhookEndpointTests.cs` |
| LIB-WEBHOOKSASPNETCORE-TEST-001 | Webhooks.AspNetCore | Behavior | Covered | `tests/Incursa.Platform.Webhooks.AspNetCore.Tests/Incursa.Platform.Webhooks.AspNetCore.Tests.csproj` |

## Next Ratchet Steps
- Stand up dedicated tests for Idempotency and Metrics libraries.
- Add non-provider mutation configs for Operations and Webhooks once baseline runtime stabilizes.
