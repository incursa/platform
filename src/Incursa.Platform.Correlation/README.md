# Incursa.Platform.Correlation

`Incursa.Platform.Correlation` provides primitives for propagating correlation context across background workflows and HTTP boundaries.

## Install

```bash
dotnet add package Incursa.Platform.Correlation
```

## What You Get

- Correlation ID value types and context objects
- Context accessor abstractions for ambient/request-scoped usage
- Header serialization helpers for cross-service propagation

## Typical Use

Use this package when you need consistent identifiers for tracing, diagnostics, and audit stitching across service boundaries.

## Documentation

- https://github.com/incursa/platform/blob/main/docs/correlation/README.md
- https://github.com/incursa/platform/blob/main/docs/INDEX.md