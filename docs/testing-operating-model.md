# Testing Operating Model

Date: 2026-03-08

## Purpose

This document defines the first four-lane testing model for the Incursa Platform repository.

The goal is to keep required CI checks straightforward while also producing advisory Workbench quality evidence and preserving a place for future known-issue coverage without turning the report into a merge gate.

## Canonical intent and derived outputs

- Authored testing intent is canonical at `docs/30-contracts/test-gate.contract.yaml`.
- Generated artifacts under `artifacts/quality/testing/` are derived outputs. Do not hand-edit them.
- The Workbench quality report is advisory only in this pass.

## Lane model

### Smoke lane

Purpose:
- the smallest high-signal precheck
- fast, deterministic, and non-Docker

Entrypoint:
- `pwsh -File scripts/quality/run-smoke-tests.ps1`

Current curated project set:
- `tests/Incursa.Platform.Tests`
- `tests/Incursa.Platform.Storage.Tests`
- `tests/Incursa.Platform.InMemory.Tests`
- `tests/Incursa.Platform.HealthProbe.Tests`
- `tests/Incursa.Platform.Webhooks.Tests`

Artifacts:
- `artifacts/codex/test-results/smoke/`

Policy:
- required in CI

### Blocking lane

Purpose:
- the main CI-safe validation lane for `Incursa.Platform.CI.slnx`
- non-Docker, non-`KnownIssue` coverage of the maintained solution

Entrypoint:
- `pwsh -File scripts/quality/run-blocking-tests.ps1`

Runsettings:
- `runsettings/blocking.runsettings`

Artifacts:
- `artifacts/codex/test-results/blocking/`

Policy:
- required in CI

### Observational lane

Purpose:
- visible, runnable tests that expose real product gaps without blocking delivery

Entrypoint:
- `pwsh -File scripts/quality/run-observational-tests.ps1`

Runsettings:
- `runsettings/observational.runsettings`

Artifacts:
- `artifacts/codex/test-results/observational/`

Policy:
- non-blocking in CI

Current state:
- no tests are currently tagged into the `KnownIssue` lane
- that empty lane is intentional for this pass; the taxonomy and artifact path are now in place

### Advisory lane

Purpose:
- produce broader confidence evidence for Workbench
- keep quality summaries visible without gating merges or releases

Entrypoints:
- raw evidence only: `pwsh -File scripts/quality/run-advisory-quality-tests.ps1`
- full advisory flow: `pwsh -File scripts/quality/run-quality-evidence.ps1`

Inputs:
- blocking-style TRX results from the advisory lane
- curated advisory coverage from `run-library-coverage.ps1`
- curated advisory provider coverage from `run-provider-coverage.ps1`

Artifacts:
- `artifacts/codex/test-results/advisory/`
- `artifacts/codex/coverage/advisory/`
- `artifacts/quality/testing/`

Policy:
- non-blocking in CI

## Artifact model

Stable output roots:

- smoke results: `artifacts/codex/test-results/smoke/`
- blocking results: `artifacts/codex/test-results/blocking/`
- observational results: `artifacts/codex/test-results/observational/`
- advisory results: `artifacts/codex/test-results/advisory/`
- advisory coverage: `artifacts/codex/coverage/advisory/`
- normalized Workbench outputs: `artifacts/quality/testing/`

Workbench ingestion for this repo is intentionally pointed at the advisory lane:

```powershell
dotnet tool run workbench quality sync `
  --contract docs/30-contracts/test-gate.contract.yaml `
  --results artifacts/codex/test-results/advisory `
  --coverage artifacts/codex/coverage/advisory `
  --out-dir artifacts/quality/testing
```

## Test taxonomy guidance

Existing repo categories remain the base contract:

- `Category=Unit`
- `Category=Integration`
- `RequiresDocker=true`

New lane guidance:

- use `Category=KnownIssue` only for runnable tests that expose a real product or architecture gap
- do not use `KnownIssue` for flaky tests, broken harnesses, or secret-dependent/manual-only checks
- a dedicated `Smoke` trait is not required for this first pass; smoke is currently defined by a curated project list plus `runsettings/smoke.runsettings`

Known issues currently tracked for automation are listed in `docs/testing-known-issues.md`.

## Happy paths

Run the required CI-safe lane locally:

```powershell
pwsh -File scripts/quality/run-blocking-tests.ps1
```

Run the advisory raw-evidence lane locally:

```powershell
pwsh -File scripts/quality/run-advisory-quality-tests.ps1
```

Run the full advisory Workbench flow:

```powershell
pwsh -File scripts/quality/run-quality-evidence.ps1
```

Run the steps individually:

```powershell
dotnet tool restore
pwsh -File scripts/quality/run-advisory-quality-tests.ps1
dotnet tool run workbench quality sync --contract docs/30-contracts/test-gate.contract.yaml --results artifacts/codex/test-results/advisory --coverage artifacts/codex/coverage/advisory --out-dir artifacts/quality/testing
dotnet tool run workbench quality show
```

## CI shape

- smoke runs first and is a hard gate
- blocking, observational, and advisory fan out after smoke
- observational and advisory are non-blocking
- publish depends on blocking, not on observational or advisory

## Intentional first-pass limits

This pass intentionally does not:

- retag the entire suite with new smoke metadata
- add mutation or fuzzing lanes
- require Docker-backed evidence in the default path
- fail merges because of the Workbench report
- hand-normalize every historical or partially detached test asset
