# Incursa.Platform.InMemory

`Incursa.Platform.InMemory` provides non-durable in-memory implementations of Incursa Platform primitives for tests and local development.

## Install

```bash
dotnet add package Incursa.Platform.InMemory
```

## What You Get

- In-memory outbox, inbox, scheduler, fanout, and lease implementations
- Provider implementations compatible with core abstractions
- Fast setup for local experiments and unit/integration tests without external infrastructure

## Notes

- State is process-local and non-durable.
- This package is not suitable for production workloads requiring cross-process coordination.

## Documentation

- https://github.com/incursa/platform/blob/main/docs/testing/README.md
- https://github.com/incursa/platform/blob/main/docs/INDEX.md