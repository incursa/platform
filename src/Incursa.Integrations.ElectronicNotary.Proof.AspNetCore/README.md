# Incursa.Integrations.ElectronicNotary.Proof.AspNetCore

`Incursa.Integrations.ElectronicNotary.Proof.AspNetCore` is the ASP.NET Core host package for the Electronic Notary proof integration.

## What It Owns

- proof webhook endpoint hosting and dispatch
- webhook authentication and classification for proof callbacks
- healing and replay-oriented hosted services and persistence hooks
- ASP.NET Core service registration for the proof integration

## What It Does Not Own

- the proof API client itself
- provider-neutral webhook contracts
- a generalized document-signing capability model

## When To Use It

Use this package when an ASP.NET Core application needs to receive proof callbacks, dispatch them to handlers, and optionally run provider-specific healing flows for incomplete or delayed transactions.

## Related Packages

- `Incursa.Integrations.ElectronicNotary.Proof`
- `Incursa.Platform.Webhooks`
