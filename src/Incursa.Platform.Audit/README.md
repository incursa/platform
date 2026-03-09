# Incursa.Platform.Audit

`Incursa.Platform.Audit` provides the provider-neutral audit-event model for the monorepo. It gives capabilities and integrations a common way to record immutable audit history without coupling the public surface to any single sink or storage implementation.

## What It Owns

- immutable audit-event contracts
- actors, targets, outcomes, and anchor modeling
- reader and writer abstractions for audit stores
- shared validation helpers for stable audit payloads and metadata

## What It Does Not Own

- provider-specific delivery or export APIs
- access-specific audit policy
- application-specific event naming conventions beyond the shared primitives

## Related Packages

- `Incursa.Platform.Access` for access-domain audit journaling
- `Incursa.Integrations.WorkOS.Audit` for asynchronous WorkOS delivery
- `Incursa.Platform.Observability` for adjacent operation and tracing conventions

## Typical Use

Use this package when you want a stable audit contract in core code while keeping storage or provider choices in adapter packages.
