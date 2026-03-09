# Quality Scripts

## Scripts
- `scripts/quality/run-smoke-tests.ps1`
  - Runs the curated fast smoke suite from the explicit `Category=Smoke` test set in the maintained smoke projects.
  - Default output: `artifacts/codex/test-results/smoke/`
- `scripts/quality/run-blocking-tests.ps1`
  - Runs the required CI-safe non-Docker lane against `Incursa.Platform.CI.slnx`.
  - Default output: `artifacts/codex/test-results/blocking/`
- `scripts/quality/run-observational-tests.ps1`
  - Runs `Category=KnownIssue` tests without blocking the overall process.
  - Default output: `artifacts/codex/test-results/observational/`
- `scripts/quality/run-advisory-quality-tests.ps1`
  - Produces advisory TRX and curated Cobertura evidence without syncing Workbench.
  - Default outputs: `artifacts/codex/test-results/advisory/` and `artifacts/codex/coverage/advisory/`
- `scripts/quality/run-quality-evidence.ps1`
  - Runs the advisory lane, syncs Workbench quality evidence, and shows the latest advisory report.
  - Default outputs: `artifacts/codex/test-results/advisory/`, `artifacts/codex/coverage/advisory/`, and `artifacts/quality/testing/`
- `scripts/quality/run-workbench-evidence.ps1`
  - Compatibility entrypoint that forwards to the advisory evidence runner.
  - Default outputs: `artifacts/codex/test-results/advisory/` and `artifacts/codex/coverage/advisory/`
- `scripts/quality/run-provider-coverage.ps1`
  - Runs provider-oriented coverage gates for selected targets.
  - Supports `-Targets` (`InMemory`, `SqlServer`, `Postgres`), line threshold, optional branch threshold (`-BranchThreshold`), and custom output roots.
- `scripts/quality/run-provider-mutation.ps1`
  - Runs scoped Stryker mutation tests using provider configs.
  - Requires local tool `dotnet-stryker`.
- `scripts/quality/validate-provider-traceability.ps1`
  - Verifies that provider `PRIM-*` scenario IDs in specs are fully represented in the conformance matrix.
  - Validates matrix mapped test file paths for `Covered` rows.
- `scripts/quality/run-library-coverage.ps1`
  - Runs cross-library unit coverage gates for configurable targets.
  - Supports line threshold, optional branch threshold, and custom output roots.
- `scripts/quality/run-library-mutation.ps1`
  - Runs required mutation configs for priority libraries and tracks deferred optional targets.
- `scripts/quality/validate-library-traceability.ps1`
  - Verifies that cross-library `LIB-*` scenario IDs in specs are fully represented in the library conformance matrix.
  - Validates matrix mapped file paths for `Covered` rows.

## Stryker Configs
- `scripts/quality/stryker/sqlserver.stryker-config.json`
- `scripts/quality/stryker/postgres.stryker-config.json`
- `scripts/quality/stryker/inmemory.stryker-config.json`

## Related Workflows
- `.github/workflows/provider-fast-quality.yml`
- PR/manual fast provider validation for traceability, coverage, and non-Docker tests
- `.github/workflows/provider-integration.yml`
- nightly/manual provider integration lane for Docker-backed SQL Server and Postgres validation
- `.github/workflows/provider-mutation.yml`
- `.github/workflows/library-fast-quality.yml`
- PR/manual fast library validation for traceability, coverage, and non-Docker tests
- `.github/workflows/workbench-quality.yml`

## Workbench Quality Workflow
- Canonical contract: `docs/30-contracts/test-gate.contract.yaml`
- Smoke: `pwsh -File scripts/quality/run-smoke-tests.ps1`
- Blocking: `pwsh -File scripts/quality/run-blocking-tests.ps1`
- Observational: `pwsh -File scripts/quality/run-observational-tests.ps1`
- Generate advisory evidence: `pwsh -File scripts/quality/run-advisory-quality-tests.ps1`
- One-command advisory sync/show: `pwsh -File scripts/quality/run-quality-evidence.ps1`
- Normalize evidence manually: `dotnet tool run workbench quality sync --contract docs/30-contracts/test-gate.contract.yaml --results artifacts/codex/test-results/advisory --coverage artifacts/codex/coverage/advisory --out-dir artifacts/quality/testing`
- Inspect the current report: `dotnet tool run workbench quality show`
- Derived outputs: `artifacts/quality/testing/`
