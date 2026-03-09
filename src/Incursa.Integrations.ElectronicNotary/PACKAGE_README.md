# Incursa.Integrations.ElectronicNotary

`Incursa.Integrations.ElectronicNotary` is the root layer 1 package family marker for the electronic-notary integration surface in this monorepo.

It exists to give the vendor family a stable public landing point and a coherent naming anchor alongside the other `Incursa.Integrations.*` packages.

## What It Owns

- the vendor-family root package for Electronic Notary integrations
- shared naming, packaging, and discovery anchor points for the related proof packages

## What It Does Not Own

- the proof client implementation
- ASP.NET Core webhook hosting or healing workflows
- provider-neutral signing or document-workflow capabilities

## Family Map

- `Incursa.Integrations.ElectronicNotary.Abstractions`
- `Incursa.Integrations.ElectronicNotary.Proof`
- `Incursa.Integrations.ElectronicNotary.Proof.AspNetCore`

## Install

```bash
dotnet add package Incursa.Integrations.ElectronicNotary
```
