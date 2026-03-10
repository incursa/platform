# Package Versioning Reset

## Decision

This repository is abandoning the old date-based NuGet package version line and resetting onto a normal semantic version line starting at `6.0.0`.

## Why the old date-based versions are being abandoned

The previous publish flow produced timestamp-like versions such as `2026.3.9.853`. That made every build look like a release event, but it did not communicate package maturity or API intent in a useful way. It also created unnecessary noise when only a subset of packages actually changed.

## Why keeping the same package IDs is acceptable

The existing package IDs are already the right public names for this repository. External adoption of the old date-based versions is minimal, so keeping the IDs avoids a disruptive rename while still letting the repository move to a clearer SemVer strategy.

## Why a lower unpublished version is still valid on NuGet.org

NuGet.org blocks publishing only when the exact package ID and exact package version already exist together. It does not require every newly published version to be numerically greater than every prior version for that package ID. That means an unpublished version like `6.0.0` is valid even if older published history includes higher-looking timestamp versions such as `2026.3.9.853`.

Unlisting older versions later is still useful for discovery, but that is a separate publishing step and not a prerequisite for starting the new SemVer line.

## Why `6.0.0` was chosen

This codebase does not look brand new. The local git history shows more than 200 commits since the initial September 2025 import, multiple substantial architecture shifts, public package families, provider implementations, analyzers, and a full monorepo consolidation. A baseline of `1.0.0` would undersell that maturity, while a giant artificial version such as `5000.0.0` or `10000.0.0` would be mechanically functional but visually misleading.

`6.0.0` is a pragmatic reset point: it reads like an established library line, acknowledges that the public surface has been through several iterations already, and avoids pretending there is a precise historical mapping from the old date-based scheme into SemVer majors and minors.

## Follow-up publishing work

- Existing date-based versions may later be unlisted or deprecated on NuGet.org.
- That is a separate manual publishing/maintenance step.
- The repository itself should continue from the `6.0.0` line using normal SemVer bumps.
