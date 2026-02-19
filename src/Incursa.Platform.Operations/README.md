# Incursa.Platform.Operations

`Incursa.Platform.Operations` provides provider-agnostic operation tracking primitives for long-running workflows.

## Install

```bash
dotnet add package Incursa.Platform.Operations
```

## What You Get

- Operation identifiers and status models
- Tracker and watcher interfaces for progress/events
- Snapshot/event contracts for querying current and historical state

## Typical Use

Use this package to model operation progress and lifecycle state independent of your chosen persistence implementation.

## Documentation

- https://github.com/incursa/platform/blob/main/docs/operations/README.md
- https://github.com/incursa/platform/blob/main/docs/INDEX.md