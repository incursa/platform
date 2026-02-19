# Incursa.Platform.Audit

`Incursa.Platform.Audit` provides provider-agnostic primitives for recording and querying immutable audit events.

## Install

```bash
dotnet add package Incursa.Platform.Audit
```

## What You Get

- Strongly typed audit event models (`AuditEvent`, `AuditActor`, anchors, outcomes)
- Validation helpers for consistent payloads and metadata
- Reader/writer abstractions for provider-specific implementations

## Typical Use

Use this package when you want a stable audit contract in core code while keeping storage/provider choices in adapter packages.

## Documentation

- https://github.com/incursa/platform/blob/main/docs/audit/README.md
- https://github.com/incursa/platform/blob/main/docs/INDEX.md