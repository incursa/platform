# Provider Quality Baseline

## Snapshot Date
- 2026-02-20

## Priority Libraries
- `src/Incursa.Platform.SqlServer`
- `src/Incursa.Platform.Postgres`
- `src/Incursa.Platform.InMemory`

## Baseline Signals
- Solution map generated: `artifacts/codex/solution-map.md`
- Build diagnostics artifact generated: `artifacts/codex/build-summary.txt`
- Test triage artifacts generated:
  - `artifacts/codex/test-failures.md`
  - `artifacts/codex/test-filter.txt`

## Current Risks
- Build analyzer failures were observed in `Incursa.Platform.Observability` during baseline capture.
- Integration test failures were dominated by missing Docker images:
  - `mcr.microsoft.com/mssql/server:2022-CU10-ubuntu-22.04`
  - `postgres:16-alpine`
- Coverage and mutation gates were not previously enforced for provider libraries.

## Initial Guardrails Added
- Fast quality lane workflow: `.github/workflows/provider-fast-quality.yml`
- Integration lane workflow: `.github/workflows/provider-integration.yml`
- Scoped mutation workflow: `.github/workflows/provider-mutation.yml`
- Docker pre-pull script: `scripts/testing/prepull-test-images.ps1`
- Provider coverage script: `scripts/quality/run-provider-coverage.ps1`
- Provider mutation script: `scripts/quality/run-provider-mutation.ps1`

## Next Ratchet Targets
- Coverage gate start: line >= 30 for provider unit tests (initial ratchet baseline).
- Branch coverage gating: deferred to next ratchet step once provider unit-test density is higher.
- Mutation gate start: break threshold >= 45 on scoped critical files.
