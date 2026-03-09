# Incursa.Platform.Webhooks

`Incursa.Platform.Webhooks` provides provider-agnostic webhook ingestion and processing primitives. It is the layer 2 home for receiving, authenticating, classifying, deduplicating, and processing external provider events without baking provider-specific assumptions into the core workflow.

## What It Owns

- contracts for webhook classification, authentication, and parsing
- ingestion and processing pipeline abstractions
- retry-friendly primitives for durable webhook handling

## What It Does Not Own

- ASP.NET Core endpoint plumbing
- provider-specific signature formats or event schemas
- application-specific event handlers

## Related Packages

- `Incursa.Platform.Webhooks.AspNetCore` for HTTP hosting integration
- `Incursa.Integrations.WorkOS.Webhooks` for WorkOS-specific signature and payload translation

## Typical Use

Use this package to define provider-independent webhook workflows, then add host-specific integration via `Incursa.Platform.Webhooks.AspNetCore`.
