# Incursa Integrations: Electronic Notary

NuGet packages for integrating Incursa applications with electronic notary providers, including webhook ingestion, signature validation, and ASP.NET Core registration helpers.

## Packages

- `Incursa.Integrations.ElectronicNotary.Abstractions`
- `Incursa.Integrations.ElectronicNotary`
- `Incursa.Integrations.ElectronicNotary.Proof`
- `Incursa.Integrations.ElectronicNotary.Proof.AspNetCore`

## What This Provides

- Provider-agnostic contracts for notary events and processing.
- Proof-specific integration components.
- ASP.NET Core endpoint wiring for webhook ingestion.
- Idempotent processing patterns compatible with the platform inbox/outbox pipeline.

## Build And Test

```bash
dotnet restore Incursa.Integrations.ElectronicNotary.slnx
dotnet build Incursa.Integrations.ElectronicNotary.slnx -c Release
dotnet test --solution Incursa.Integrations.ElectronicNotary.slnx -c Release
```

## Quality Gates

```powershell
pwsh -File scripts/verify-spec-traceability.ps1
pwsh -File scripts/quality/run-quality-gates.ps1 -Solution Incursa.Integrations.ElectronicNotary.slnx
```

Quality references:

- Specs: `docs/specs/`
- Traceability: `docs/testing/traceability.md`
- Thresholds: `docs/testing/quality-thresholds.md`
- Compatibility decisions: `docs/compatibility/decisions.md`

## Pack

```powershell
pwsh -File scripts/pack.ps1 -Version 1.0.0
```

Packages are emitted under `artifacts/packages`.

## Release

Create and push a SemVer tag (for example `v1.0.20`). The release workflow builds, tests, packs, and publishes packages.
