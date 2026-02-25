# Incursa.Platform.Audit.WorkOS

Outbox-backed WorkOS sink for `Incursa.Platform.Audit`.

## Install

```bash
dotnet add package Incursa.Platform.Audit.WorkOS
```

## What it provides

- `AddAuditSinkFanout()` decorator to fan out persisted audit events into sink outbox topics.
- `AddWorkOsAuditSink(...)` to register WorkOS sink serialization + outbox handler.
- Asynchronous delivery through existing platform `IOutbox`/`IOutboxHandler` infrastructure.

## Defaults

- Local audit store remains source of truth.
- WorkOS delivery is asynchronous and retryable.
- Mapping failures are permanent failures for the sink outbox item only.
