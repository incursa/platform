---
workbench:
  type: runbook
  workItems:
    - TASK-0002
  codeRefs:
    - /eng/Pack-PublicPackages.ps1
    - /eng/Test-PackageArtifacts.ps1
    - /.github/workflows/main-release-ci-cd.yml
  pathHistory: []
  path: /docs/50-runbooks/nuget-releases.md
---

# NuGet releases

The package catalog is the publication allowlist. Every publishable entry also belongs to exactly one release group so unrelated tools and support packages are not released accidentally with the production platform libraries.

## Release groups

| Group | Contents | Publication intent |
| --- | --- | --- |
| `platform` | All publishable projects under `src/` | Coordinated production library release |
| `test-support` | `Incursa.Platform.TestUtilities` | Downstream provider and integration test projects |
| `observability-tools` | `Incursa.Platform.Observability.Analyzers` | Independent analyzer release |
| `prose-tools` | `Incursa.Prose.Cli` | Independent .NET tool release |
| `testdocs-tools` | `Incursa.TestDocs.Analyzers` and `Incursa.TestDocs.Cli` | Coordinated TestDocs tooling release |

`Incursa.Platform.TestUtilities` is intentionally public. Provider test suites in sibling repositories consume its reusable behavior tests and fakes through NuGet, avoiding cross-repository project references.

## Validation

Pack and validate the complete public surface:

```powershell
pwsh -File eng/Generate-PackageCatalog.ps1
pwsh -File eng/Pack-PublicPackages.ps1 -Configuration Release -OutputPath ./nupkgs -PublishableOnly
pwsh -File eng/Test-PackageArtifacts.ps1 -PackagePath ./nupkgs -RequireAllPublishable
```

Preview or pack one group:

```powershell
pwsh -File eng/Pack-PublicPackages.ps1 -Configuration Release -OutputPath ./release-nupkgs -PublishableOnly -ReleaseGroup platform -DryRun
```

The artifact validator checks the generated `.nuspec` plus the contents of each `.nupkg`. It requires the package title, authors, description, project URL, tags, SPDX license expression, git repository, icon, and README, and verifies that the declared icon and README are actually embedded.

## Publication

Use the `Main/Release CI/CD` manual workflow and select one release group. Enabling `publish_packages` publishes only that group. A `v*` tag is reserved for coordinated `platform` releases and therefore publishes only the `platform` group.

Do not push the general `nupkgs` artifact directly. It intentionally contains the complete allowlisted surface for CI verification, while the `release-nupkgs` artifact contains only the selected publication group.
