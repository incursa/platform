# Incursa.Platform.Email

`Incursa.Platform.Email` provides provider-agnostic email outbox primitives and dispatch workflows.

## Install

```bash
dotnet add package Incursa.Platform.Email
```

## What You Get

- Email outbox contracts, models, and processing abstractions
- Dispatch outcomes, policy hooks, and delivery sink interfaces
- Building blocks for reliable, retriable, idempotent email pipelines

## Typical Use

Use this package for application-level email orchestration, then pair it with a provider adapter package from an integration repository (for example `Incursa.Integrations.Postmark`).

## Documentation

- https://github.com/incursa/platform/blob/main/docs/email/README.md
- https://github.com/incursa/platform/blob/main/docs/INDEX.md
