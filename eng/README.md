# Engineering Assets

This directory holds monorepo governance and release helpers for `Incursa.Platform`.

## Files

- `package-catalog.json`
  Authoritative project classification and pack/publish allowlist.
- `Generate-PackageCatalog.ps1`
  Regenerates the catalog from the current project tree and monorepo classification rules.
- `Resolve-AffectedProjects.ps1`
  Resolves changed projects and their reverse project-reference dependents.
- `Pack-PublicPackages.ps1`
  Packs only the allowlisted projects from the catalog, optionally limited to affected projects.

## Usage

```powershell
pwsh -File eng/Generate-PackageCatalog.ps1
pwsh -File eng/Resolve-AffectedProjects.ps1 -Base origin/main -Head HEAD
pwsh -File eng/Pack-PublicPackages.ps1 -Configuration Release -OutputPath ./nupkgs
pwsh -File eng/Pack-PublicPackages.ps1 -Configuration Release -OutputPath ./nupkgs -AffectedOnly -Base origin/main -Head HEAD
pwsh -File eng/Pack-PublicPackages.ps1 -Configuration Release -OutputPath ./nupkgs -PublishableOnly
```
