# Incursa.Integrations.ElectronicNotary.Proof

`Incursa.Integrations.ElectronicNotary.Proof` is the vendor-specific proof client package for the Electronic Notary family.

## What It Owns

- typed request and response models for the proof API
- proof transaction and signer identifiers
- proof webhook signature verification primitives
- service registration for the proof client and related telemetry hooks

## What It Does Not Own

- provider-neutral document-signing abstractions
- ASP.NET Core endpoint hosting or healing workers
- a generalized webhook pipeline

## Typical Use

Use this package in services that need to create proof transactions, register documents and signers, and verify webhook signatures against the provider's expectations.

## Related Packages

- `Incursa.Integrations.ElectronicNotary`
- `Incursa.Integrations.ElectronicNotary.Proof.AspNetCore`
