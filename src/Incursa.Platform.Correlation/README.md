# Incursa.Platform.Correlation

`Incursa.Platform.Correlation` provides the shared correlation primitives used to connect HTTP requests, background workflows, operations, and audit trails.

## What It Owns

- correlation ID value types and context objects
- context accessor abstractions for ambient and request-scoped usage
- header serialization helpers for cross-service propagation

## What It Does Not Own

- tracing exporters or vendor-specific telemetry sinks
- operation history storage
- audit records themselves

## Related Packages

- `Incursa.Platform.Observability` for shared event and tag conventions
- `Incursa.Platform.Operations` for long-running workflow tracking
- `Incursa.Platform.Audit` for immutable audit history

## Typical Use

Use this package when you need consistent identifiers for tracing, diagnostics, and audit stitching across service boundaries.
