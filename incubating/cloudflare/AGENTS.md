# Repository Guidelines

## Early Repo Orientation (run first)
We do not have LSP support in some environments. Build a semantic map first.

- Read `llms.txt` for the curated repository map and core commands.
- Repo discovery: `pwsh -File .codex/skills/dotnet-repo-discovery/scripts/discover-dotnet-repo.ps1`
- Build diagnostics: `pwsh -File .codex/skills/dotnet-build-diagnostics/scripts/run-build-diagnostics.ps1`
- Test triage (when tests are involved): `bash .codex/skills/dotnet-test-triage/scripts/run-test-triage.sh`
- Format/analyzers (before finalizing C# changes): `pwsh -File .codex/skills/dotnet-format-analyzers/scripts/run-format-analyzers.ps1`
- Symbol grep recipes (during investigation): `rg -n "\b(class|interface|record|struct)\s+<Name>\b" -g "*.cs"`

Decision checklist:
- If the task mentions compiler/build failures, run build diagnostics.
- If tests fail or test projects are modified, run test triage.
- If C# files are added/modified, run format/analyzers before final output.

Expected artifacts:
- `artifacts/codex/solution-map.json` and `artifacts/codex/solution-map.md`
- `artifacts/codex/build.binlog` and `artifacts/codex/build-summary.txt`
- `artifacts/codex/test-failures.md` and `artifacts/codex/test-filter.txt`
- `artifacts/codex/format-report.txt` (when format/analyzers are run)

## Conventions
- Libraries live under `src/` and are packable.
- Tests live under `tests/` and should be fast and isolated.
- Keep template assets generic; avoid app-specific paths or product-specific assumptions.
- Prefer small, reviewable PRs with clear testing notes.

## Workbench Baseline
- Workbench repo config lives at `.workbench/config.json`.
- Workbench authoring scaffold lives under `docs/` and `docs/70-work/`.
- Workbench Codex skills live under `skills/`:
  - `skills/workbench-docs`
  - `skills/workbench-architecture`
  - `skills/workbench-work-items`
  - `skills/workbench-sync`
  - `skills/workbench-github`
- Daily automation workflows:
  - `.github/workflows/workbench-sync-normalize.yml`
  - `.github/workflows/workbench-item-issue-sync.yml`

## PR Quality Contract
- Use spec-first behavior, then tests, then implementation.
- Do not change public behavior without updating docs/spec notes in the same PR.
- Keep PRs small and focused; avoid unrelated churn.
- Do not lower quality gates to make a change pass.

### Required Local Gates
- `dotnet format --verify-no-changes <solution>`
- `dotnet build <solution> -c Release --no-restore -warnaserror -p:ContinuousIntegrationBuild=true`
- `dotnet test --solution <solution> -c Release --no-build`
- Coverage gate checks in CI (line + branch thresholds from repo variables)

### Traceability Expectations
- For changed public contracts, include:
  - expected valid inputs and invalid inputs,
  - normalization/error behavior,
  - tests that map to changed behavior.
