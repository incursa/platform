---
id: TASK-0002
type: task
status: done
priority: high
owner: platform
created: 2026-11-23
updated: 2026-11-23
tags:
  - nuget
  - release
  - packaging
related:
  specs: []
  adrs: []
  files:
    - /docs/50-runbooks/nuget-releases.md
    - /eng/Pack-PublicPackages.ps1
    - /eng/Test-PackageArtifacts.ps1
    - /.github/workflows/main-release-ci-cd.yml
  prs:
    - "https://github.com/incursa/platform/pull/139"
  issues: []
  branches:
    - codex/package-release-cleanup
title: Harden NuGet package metadata and release groups
---

# TASK-0002 - Harden NuGet package metadata and release groups

## Summary

Make the public package surface explicit and verifiable before the first coordinated semantic-version release. Preserve `Incursa.Platform.TestUtilities` as intentional downstream test support, separate production libraries from test support and tools during publication, and validate the metadata and embedded assets in the actual NuGet artifacts.

## Acceptance criteria

- Classify every publishable package into a named release group.
- Publish production platform libraries independently from test support and tools.
- Retain `Incursa.Platform.TestUtilities` as a public package because downstream provider test projects consume it through NuGet.
- Supply consistent authorship, ownership, repository, license, icon, README, tags, and related NuGet metadata defaults.
- Validate packed `.nupkg` files for required metadata and embedded icon/README assets before publication.
- Document the release groups and safe release workflow.
- Build and pack the complete public package surface successfully.

## Validation notes

- Confirmed `Incursa.Platform.TestUtilities` is consumed as a NuGet package by five provider test projects in `integrations-public`.
- Generated and validated all 30 publishable NuGet artifacts at their planned semantic versions; every package contains its declared PNG icon and README and supplies the required metadata.
- Verified the `platform` release group contains exactly 25 production packages and excludes test support and tools.
- Verified the remaining release groups contain one test-support package, one observability tool, one prose tool, and two TestDocs tools.
- The complete CI solution builds with zero warnings and errors.
- All 245 tests pass, with no failures recorded by the repository test-triage helper.
