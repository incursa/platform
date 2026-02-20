# Provider Quality Scripts

## Scripts
- `scripts/quality/run-provider-coverage.ps1`
  - Runs provider-oriented unit-test coverage gates.
  - Default thresholds: line `65`, branch `50`.
- `scripts/quality/run-provider-mutation.ps1`
  - Runs scoped Stryker mutation tests using provider configs.
  - Requires local tool `dotnet-stryker`.

## Stryker Configs
- `scripts/quality/stryker/sqlserver.stryker-config.json`
- `scripts/quality/stryker/postgres.stryker-config.json`
- `scripts/quality/stryker/inmemory.stryker-config.json`

## Related Workflows
- `.github/workflows/provider-fast-quality.yml`
- `.github/workflows/provider-integration.yml`
- `.github/workflows/provider-mutation.yml`
