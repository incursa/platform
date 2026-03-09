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
