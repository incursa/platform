# Engineering Assets

This directory holds monorepo governance and release helpers for `Incursa.Platform`.

## Files

- `package-catalog.json`
  Authoritative project classification and pack/publish allowlist.
- `package-versions.json`
  Authoritative per-package semantic versions for all packable projects.
- `Generate-PackageCatalog.ps1`
  Regenerates the catalog from the current project tree and monorepo classification rules.
- `Initialize-PackageVersions.ps1`
  Bootstraps the package version manifest from the current packable projects and normalizes explicit `<Version>` nodes.
- `Resolve-AffectedProjects.ps1`
  Resolves changed projects and their reverse project-reference dependents.
- `Resolve-VersionPlan.ps1`
  Resolves the package version bump plan for changed packable projects and their packable dependents.
- `Apply-VersionPlan.ps1`
  Applies the resolved version bump plan to the manifest and packable project files.
- `Set-PackageVersionBaseline.ps1`
  Sets a one-time baseline semantic version across all packable projects and the version manifest.
- `Pack-PublicPackages.ps1`
  Packs only the allowlisted projects from the catalog, optionally limited to affected projects.
- `Test-PackageVersionChanges.ps1`
  Fails when packable project changes are present without corresponding package version bumps.

## Usage

```powershell
pwsh -File eng/Generate-PackageCatalog.ps1
pwsh -File eng/Resolve-AffectedProjects.ps1 -Base origin/main -Head HEAD
pwsh -File eng/Resolve-VersionPlan.ps1 -Base origin/main -Head HEAD -AsJson
pwsh -File eng/Apply-VersionPlan.ps1 -Base origin/main -Head HEAD
pwsh -File eng/Test-PackageVersionChanges.ps1 -Base origin/main -Head HEAD
pwsh -File eng/Set-PackageVersionBaseline.ps1 -Version 6.0.0
pwsh -File eng/Pack-PublicPackages.ps1 -Configuration Release -OutputPath ./nupkgs
pwsh -File eng/Pack-PublicPackages.ps1 -Configuration Release -OutputPath ./nupkgs -AffectedOnly -Base origin/main -Head HEAD
pwsh -File eng/Pack-PublicPackages.ps1 -Configuration Release -OutputPath ./nupkgs -PublishableOnly
```

## Recommended workflow

1. Run `pwsh -File eng/Apply-VersionPlan.ps1 -Base origin/main -Head HEAD` before packaging or opening a PR.
2. Review the package/version changes written to `eng/package-versions.json` and the affected `.csproj` files.
3. Commit those version changes alongside the code changes.
4. Enable the local pre-commit hook with `pwsh -File scripts/setup-git-hooks.ps1`.
