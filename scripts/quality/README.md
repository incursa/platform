# Provider Quality Scripts

## Scripts
- `scripts/quality/run-provider-coverage.ps1`
  - Runs provider-oriented coverage gates for selected targets.
  - Supports `-Targets` (`InMemory`, `SqlServer`, `Postgres`), line threshold, and optional branch threshold (`-BranchThreshold`).
- `scripts/quality/run-provider-mutation.ps1`
  - Runs scoped Stryker mutation tests using provider configs.
  - Requires local tool `dotnet-stryker`.
- `scripts/quality/validate-provider-traceability.ps1`
  - Verifies that provider `PRIM-*` scenario IDs in specs are fully represented in the conformance matrix.
  - Validates matrix mapped test file paths for `Covered` rows.

## Stryker Configs
- `scripts/quality/stryker/sqlserver.stryker-config.json`
- `scripts/quality/stryker/postgres.stryker-config.json`
- `scripts/quality/stryker/inmemory.stryker-config.json`

## Related Workflows
- `.github/workflows/provider-fast-quality.yml`
- `.github/workflows/provider-integration.yml`
- `.github/workflows/provider-mutation.yml`
