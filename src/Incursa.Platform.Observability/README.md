# Incursa.Platform.Observability

`Incursa.Platform.Observability` provides the shared observability vocabulary used across the platform package families.

## What It Owns

- shared event names and tag-key conventions
- lightweight helpers that bridge correlation, operations, audit, and messaging metadata
- a common vocabulary for platform telemetry without forcing a single telemetry backend

## What It Does Not Own

- a full tracing or logging stack
- exporter-specific configuration
- database-backed metric persistence on its own

## Related Packages

- `Incursa.Platform.Correlation` for correlation context propagation
- `Incursa.Platform.Operations` for workflow lifecycle tracking
- `Incursa.Platform.Audit` for immutable event history
- `Incursa.Platform.Metrics.AspNetCore` and `Incursa.Platform.Metrics.HttpServer` for metrics exposure

## Conventions

Standard event names are defined in `PlatformEventNames`.

Standard tag keys are defined in `PlatformTagKeys`.
