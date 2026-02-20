# Cross-Library Hardening Baseline

## Snapshot Date
- 2026-02-20

## Objective
Extend the provider hardening model to every public package with explicit, traceable quality requirements and incremental ratcheting.

## Scope and Priority
- Priority 1: `src/Incursa.Platform.InMemory`, `src/Incursa.Platform.SqlServer`, `src/Incursa.Platform.Postgres`
- Priority 2: `src/Incursa.Platform`, `src/Incursa.Platform.Email`, `src/Incursa.Platform.Webhooks`, `src/Incursa.Platform.Operations`, `src/Incursa.Platform.Observability`
- Priority 3: remaining extension/adapter libraries

## Current Guardrails
- Library spec: `specs/libraries/library-interface-quality-spec.md`
- Library traceability matrix: `specs/libraries/library-conformance-matrix.md`
- Library traceability validator: `scripts/quality/validate-library-traceability.ps1`
- Library coverage gate script: `scripts/quality/run-library-coverage.ps1`
- Library mutation orchestrator: `scripts/quality/run-library-mutation.ps1`
- CI lane: `.github/workflows/library-fast-quality.yml`

## Known Coverage Gaps (Tracked as Missing/Deferred)
- Direct test projects for `Idempotency`, `Metrics.AspNetCore`, and `Metrics.HttpServer`
- Non-provider mutation configs for Operations/Webhooks/Observability (tracked as deferred optional targets)

## Incremental Ratchet Plan
1. Stabilize `library-fast-quality` on PRs for two consecutive cycles.
2. Expand provider fuzz depth (inbox/scheduler state-machine invariants) for SQL Server and Postgres.
3. Add dedicated Idempotency and Metrics tests and convert matrix `Missing` entries to `Covered`.
4. Add mutation configs for Operations and Webhooks, then mark them required in `run-library-mutation.ps1`.
5. Raise line coverage threshold from 20 to 30 after sustained green runs.
