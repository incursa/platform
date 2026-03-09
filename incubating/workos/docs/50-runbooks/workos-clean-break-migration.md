---
workbench:
  type: runbook
  workItems: []
  codeRefs: []
  pathHistory:
    - "C:/docs/50-runbooks/workos-clean-break-migration.md"
  path: /docs/50-runbooks/workos-clean-break-migration.md
related: []
---

# WorkOS Clean-Break Migration

This runbook covers migration from the pre-consolidation package model to the clean-break package model.

## Effective Date

- February 23, 2026

## Breaking Changes

1. Removed package identities:
   - `Incursa.Integrations.WorkOS.Core`
   - `Incursa.Integrations.WorkOS.Persistence`
   - `Incursa.Integrations.WorkOS.AppAuth.Abstractions`
   - `Incursa.Integrations.WorkOS.AppAuth.AspNetCore`
2. Types from removed projects were recompiled into new assemblies:
   - former Core/Persistence/AppAuth.Abstractions code now ships in `Incursa.Integrations.WorkOS`
   - former AppAuth.AspNetCore code now ships in `Incursa.Integrations.WorkOS.AspNetCore`
3. Integration DI extension methods (`AddWorkOsIntegration`, `AddWorkOsInMemoryIntegration`, `AddWorkOsOidcAuthKit`, `AddWorkOsAppAuth`) now come from `Incursa.Integrations.WorkOS.AspNetCore`.

## New Package Model

- `Incursa.Integrations.WorkOS.Abstractions`
- `Incursa.Integrations.WorkOS`
- `Incursa.Integrations.WorkOS.AspNetCore`

## Migration Steps (Consumer Repositories)

1. Remove old package references:
   - `Incursa.Integrations.WorkOS.Core`
   - `Incursa.Integrations.WorkOS.Persistence`
   - `Incursa.Integrations.WorkOS.AppAuth.Abstractions`
   - `Incursa.Integrations.WorkOS.AppAuth.AspNetCore`
2. Add new package references:
   - Always add `Incursa.Integrations.WorkOS`
   - Add `Incursa.Integrations.WorkOS.AspNetCore` for ASP.NET Core hosts
   - Add `Incursa.Integrations.WorkOS.Abstractions` only if direct contract-only dependency is needed
3. Restore/build:
   - `dotnet restore`
   - `dotnet build -c Release`
4. Verify DI wiring compiles:
   - `services.AddWorkOsIntegration(...)`
   - `services.AddWorkOsInMemoryIntegration(...)`
   - `services.AddWorkOsOidcAuthKit(...)`
   - `services.AddWorkOsAppAuth(...)`
5. Run tests and triage failures:
   - Windows: `pwsh -File .codex/skills/dotnet-test-triage/scripts/run-test-triage.ps1`
   - Linux/macOS: `bash .codex/skills/dotnet-test-triage/scripts/run-test-triage.sh`

## Package Mapping Reference

- Old: `Incursa.Integrations.WorkOS.Core` -> New: `Incursa.Integrations.WorkOS`
- Old: `Incursa.Integrations.WorkOS.Persistence` -> New: `Incursa.Integrations.WorkOS`
- Old: `Incursa.Integrations.WorkOS.AppAuth.Abstractions` -> New: `Incursa.Integrations.WorkOS`
- Old: `Incursa.Integrations.WorkOS.AppAuth.AspNetCore` -> New: `Incursa.Integrations.WorkOS.AspNetCore`

## Known Compile-Time Symptom

- If extension methods are missing (for example `AddWorkOsIntegration`), add `Incursa.Integrations.WorkOS.AspNetCore` package reference to the host project.
