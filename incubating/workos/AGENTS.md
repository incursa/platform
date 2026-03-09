# Repository Guidelines

## Early Repo Orientation (run first)
We do not have LSP support in some environments. Build a semantic map first.

- Read `llms.txt` for the curated repository map and core commands.
- Repo discovery: `pwsh -File .codex/skills/dotnet-repo-discovery/scripts/discover-dotnet-repo.ps1`
- Build diagnostics: `pwsh -File .codex/skills/dotnet-build-diagnostics/scripts/run-build-diagnostics.ps1`
- Test triage (when tests are involved):
  - Windows/PowerShell: `pwsh -File .codex/skills/dotnet-test-triage/scripts/run-test-triage.ps1`
  - Linux/macOS/Bash: `bash .codex/skills/dotnet-test-triage/scripts/run-test-triage.sh`
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

## Workbench Workflow
- Use local .NET tool manifest and run Workbench through `dotnet tool run workbench -- ...`.
- Install/restore from repo root:
  - `dotnet tool restore`
  - If missing manifest: `dotnet new tool-manifest`
  - Pin/update tool intentionally: `dotnet tool install --local Incursa.Workbench --version 2026.2.20.344`
- Mode detection:
  - If `.workbench/config.json` is missing: baseline install (`init`/`scaffold` flow).
  - If Workbench exists with legacy/mixed structure: run migration flow (`migrate coherent-v1`).
  - Otherwise: maintenance flow.
- Required migration sequence (dry-run before mutate):
  - `dotnet tool run workbench -- doctor --json`
  - `dotnet tool run workbench -- migrate coherent-v1 --dry-run`
  - `dotnet tool run workbench -- migrate coherent-v1`
  - `dotnet tool run workbench -- item normalize --include-done`
  - `dotnet tool run workbench -- validate --strict`
- Daily/PR-safe checks:
  - `dotnet tool run workbench -- validate --strict`
  - `dotnet tool run workbench -- item normalize --include-done --dry-run`
  - `dotnet tool run workbench -- sync --items --issues false --dry-run`
- Work item lifecycle:
  - Create: `dotnet tool run workbench -- item new --type task --title "<title>"`
  - Update status: `dotnet tool run workbench -- item status <ID> <status>`
  - Close + move to done: `dotnet tool run workbench -- item close <ID> --move`
  - Link docs/PRs/issues: `dotnet tool run workbench -- item link <ID> ...`
- Automation workflows:
  - Verify (read-only): `.github/workflows/workbench-verify.yml`
  - Maintenance sync (scheduled/manual, PR-based mutation): `.github/workflows/workbench-sync-normalize.yml`
  - Controlled issue import (manual only): `.github/workflows/workbench-item-issue-sync.yml`
