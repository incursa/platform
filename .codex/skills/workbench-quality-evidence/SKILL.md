---
name: workbench-quality-evidence
description: Produce and inspect Incursa Workbench quality evidence for this repository using the canonical contract and artifact paths.
---

# workbench-quality-evidence

Use this when a task mentions Workbench quality, testing evidence, coverage evidence, or the authored testing-intent contract.

## Canonical inputs

- Authored intent contract: `docs/30-contracts/test-gate.contract.yaml`
- Advisory test result artifacts: `artifacts/codex/test-results/advisory/*.trx`
- Advisory coverage artifacts: `artifacts/codex/coverage/advisory/**/*.cobertura.xml`

## Derived outputs

- Normalized quality outputs: `artifacts/quality/testing/`
- Treat everything under `artifacts/` as generated output. Do not hand-edit it.

## Happy path

```powershell
dotnet tool restore
pwsh -File scripts/quality/run-advisory-quality-tests.ps1
dotnet tool run workbench quality sync --contract docs/30-contracts/test-gate.contract.yaml --results artifacts/codex/test-results/advisory --coverage artifacts/codex/coverage/advisory --out-dir artifacts/quality/testing
dotnet tool run workbench quality show
```

One-command wrapper:

```powershell
pwsh -File scripts/quality/run-quality-evidence.ps1
```

## Guidance

- The contract and linked docs are canonical. Generated Workbench summaries are not.
- Smoke and blocking lanes are required CI checks; observational and advisory are non-blocking.
- The report is advisory only. Do not add merge gates or fail a PR only because the Workbench quality report is imperfect.
- Optional Docker-backed SQL Server and Postgres evidence can be added for deeper confidence, but it is not part of the default low-friction path.
