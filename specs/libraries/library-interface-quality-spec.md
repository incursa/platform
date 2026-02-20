# Library Interface Quality Specification

## Meta
- Scope: Cross-library hardening and traceability for all public packages under `src/`
- Status: Active
- Last Updated: 2026-02-20
- Scope Owner: Platform team

## Purpose
This specification defines the minimum production-readiness contract for each platform library:
- public interface behavior is explicitly documented
- tests exist for public interface behavior
- coverage and mutation gates are automated for critical libraries
- fuzz/property coverage exists for stateful primitives and parsers

All requirements use stable scenario IDs (`LIB-*`) and are traceable via `specs/libraries/library-conformance-matrix.md`.

## Governance Requirements
- `LIB-GOV-SPEC-001`: Every public library has interface behavior requirements tracked in this spec and matrix.
- `LIB-GOV-TEST-001`: Every public library has at least one mapped automated test path, or an explicit `Missing/Deferred` matrix entry.
- `LIB-GOV-COV-001`: Coverage gate automation exists for cross-library unit-test targets.
- `LIB-GOV-MUT-001`: Mutation gate automation exists with explicit required targets and deferred backlog targets.
- `LIB-GOV-FUZZ-001`: Fuzz/property testing requirements are explicitly tracked for critical libraries.

## Priority Library Requirements

### InMemory
- `LIB-INMEMORY-API-001`: Public registration/options surface is contract-tested.
- `LIB-INMEMORY-TEST-001`: Primitive behavior suites cover outbox/inbox/scheduler/lease invariants.
- `LIB-INMEMORY-FUZZ-001`: Randomized primitive sequence fuzzing verifies terminal-state safety.
- `LIB-INMEMORY-MUT-001`: Mutation config exists and runs in CI-scoped mutation lane.

### SqlServer
- `LIB-SQLSERVER-API-001`: Public registration/options and schema lifecycle interfaces are contract-tested.
- `LIB-SQLSERVER-TEST-001`: Primitive behavior suites cover outbox/inbox/scheduler/lease semantics.
- `LIB-SQLSERVER-FUZZ-001`: Fuzz/property coverage is tracked for queue and claim invariants.
- `LIB-SQLSERVER-MUT-001`: Mutation config exists and runs in CI-scoped mutation lane.

### Postgres
- `LIB-POSTGRES-API-001`: Public registration/options and schema lifecycle interfaces are contract-tested.
- `LIB-POSTGRES-TEST-001`: Primitive behavior suites cover outbox/inbox/scheduler/lease semantics.
- `LIB-POSTGRES-FUZZ-001`: Fuzz/property coverage is tracked for queue and claim invariants.
- `LIB-POSTGRES-MUT-001`: Mutation config exists and runs in CI-scoped mutation lane.

## Cross-Library Public Interface Requirements
- `LIB-CORE-API-001`: `Incursa.Platform` public behavior is validated by contract and regression tests.
- `LIB-CORE-TEST-001`: `Incursa.Platform` has automated unit-test coverage for public orchestration behavior.

- `LIB-AUDIT-API-001`: `Incursa.Platform.Audit` public model behavior is specified and tested.
- `LIB-AUDIT-TEST-001`: `Incursa.Platform.Audit` has automated unit tests for public contracts.

- `LIB-CORRELATION-API-001`: `Incursa.Platform.Correlation` public model behavior is specified and tested.
- `LIB-CORRELATION-TEST-001`: `Incursa.Platform.Correlation` has automated unit tests for public contracts.

- `LIB-EMAIL-API-001`: `Incursa.Platform.Email` public send/validation behavior is specified and tested.
- `LIB-EMAIL-TEST-001`: `Incursa.Platform.Email` has automated unit tests for public contracts.

- `LIB-EMAILASPNETCORE-API-001`: `Incursa.Platform.Email.AspNetCore` extension behavior is specified and tested.
- `LIB-EMAILASPNETCORE-TEST-001`: `Incursa.Platform.Email.AspNetCore` has automated tests for registration/runtime behavior.

- `LIB-EMAILPOSTMARK-API-001`: `Incursa.Platform.Email.Postmark` public adapter behavior is specified and tested.
- `LIB-EMAILPOSTMARK-TEST-001`: `Incursa.Platform.Email.Postmark` has automated unit tests for public contracts.

- `LIB-EXACTLYONCE-API-001`: `Incursa.Platform.ExactlyOnce` public execution behavior is specified and tested.
- `LIB-EXACTLYONCE-TEST-001`: `Incursa.Platform.ExactlyOnce` has automated tests for public contracts.

- `LIB-HEALTHPROBE-API-001`: `Incursa.Platform.HealthProbe` CLI and probe behavior is specified and tested.
- `LIB-HEALTHPROBE-TEST-001`: `Incursa.Platform.HealthProbe` has automated unit tests for public contracts.

- `LIB-IDEMPOTENCY-API-001`: `Incursa.Platform.Idempotency` public store behavior is specified and tested.
- `LIB-IDEMPOTENCY-TEST-001`: `Incursa.Platform.Idempotency` has automated tests for public contracts.

- `LIB-METRICSASPNETCORE-API-001`: `Incursa.Platform.Metrics.AspNetCore` extension behavior is specified and tested.
- `LIB-METRICSASPNETCORE-TEST-001`: `Incursa.Platform.Metrics.AspNetCore` has automated tests for public contracts.

- `LIB-METRICSHTTPSERVER-API-001`: `Incursa.Platform.Metrics.HttpServer` runtime behavior is specified and tested.
- `LIB-METRICSHTTPSERVER-TEST-001`: `Incursa.Platform.Metrics.HttpServer` has automated tests for public contracts.

- `LIB-MODULARITY-API-001`: `Incursa.Platform.Modularity` engine and registry behavior is specified and tested.
- `LIB-MODULARITY-TEST-001`: `Incursa.Platform.Modularity` has automated tests for public contracts.

- `LIB-MODULARITYASPNETCORE-API-001`: `Incursa.Platform.Modularity.AspNetCore` hosting extension behavior is specified and tested.
- `LIB-MODULARITYASPNETCORE-TEST-001`: `Incursa.Platform.Modularity.AspNetCore` has automated tests for public contracts.

- `LIB-MODULARITYRAZOR-API-001`: `Incursa.Platform.Modularity.Razor` Razor integration behavior is specified and tested.
- `LIB-MODULARITYRAZOR-TEST-001`: `Incursa.Platform.Modularity.Razor` has automated tests for public contracts.

- `LIB-OBSERVABILITY-API-001`: `Incursa.Platform.Observability` public conventions and emitters are specified and tested.
- `LIB-OBSERVABILITY-TEST-001`: `Incursa.Platform.Observability` has automated unit tests for public contracts.

- `LIB-OPERATIONS-API-001`: `Incursa.Platform.Operations` public model/tracker behavior is specified and tested.
- `LIB-OPERATIONS-TEST-001`: `Incursa.Platform.Operations` has automated unit tests for public contracts.

- `LIB-WEBHOOKS-API-001`: `Incursa.Platform.Webhooks` ingestion/processor behavior is specified and tested.
- `LIB-WEBHOOKS-TEST-001`: `Incursa.Platform.Webhooks` has automated unit tests for public contracts.

- `LIB-WEBHOOKSASPNETCORE-API-001`: `Incursa.Platform.Webhooks.AspNetCore` endpoint/hosted-service behavior is specified and tested.
- `LIB-WEBHOOKSASPNETCORE-TEST-001`: `Incursa.Platform.Webhooks.AspNetCore` has automated unit tests for public contracts.
