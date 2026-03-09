# Incursa.Platform.Operations

`Incursa.Platform.Operations` provides the provider-neutral operation tracking model for long-running workflows, background jobs, and other observable unit-of-work lifecycles.

## What It Owns

- operation identifiers and status models
- tracker and watcher interfaces for progress and events
- snapshot and event contracts for querying current and historical state

## What It Does Not Own

- vendor-specific workflow engines
- orchestration transport layers
- application-specific job definitions

## Related Packages

- `Incursa.Platform.Observability` for event and tag naming conventions
- `Incursa.Platform.Correlation` for cross-boundary correlation
- `Incursa.Platform.Audit` when operation milestones must become immutable audit records

## Typical Use

Use this package to model operation progress and lifecycle state independent of your chosen persistence implementation.
