# Incursa.Platform.Webhooks

`Incursa.Platform.Webhooks` provides provider-agnostic webhook ingestion and processing primitives.

## Install

```bash
dotnet add package Incursa.Platform.Webhooks
```

## What You Get

- Contracts for webhook classification, authentication, and parsing
- Ingestion and processing pipeline abstractions
- Retry-friendly primitives for durable webhook handling

## Typical Use

Use this package to define provider-independent webhook workflows, then add host-specific integration via `Incursa.Platform.Webhooks.AspNetCore`.

## Documentation

- https://github.com/incursa/platform/blob/main/docs/webhooks/README.md
- https://github.com/incursa/platform/blob/main/docs/INDEX.md