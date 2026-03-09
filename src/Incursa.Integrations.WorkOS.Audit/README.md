# Incursa.Integrations.WorkOS.Audit

`Incursa.Integrations.WorkOS.Audit` is the WorkOS-facing audit sink for `Incursa.Platform.Audit`.

It lets the platform audit capability remain the local source of truth while asynchronously forwarding selected events into WorkOS.

## Install

```bash
dotnet add package Incursa.Integrations.WorkOS.Audit
```

## What it provides

- `AddAuditSinkFanout()` decorator to fan out persisted audit events into sink outbox topics.
- `AddWorkOsAuditSink(...)` to register WorkOS sink serialization + outbox handler.
- Asynchronous delivery through existing platform `IOutbox`/`IOutboxHandler` infrastructure.

## What It Does Not Own

- the primary audit journal or canonical audit data model
- synchronous in-request delivery guarantees
- generalized audit abstractions unrelated to WorkOS

## Defaults

- Local audit store remains source of truth.
- WorkOS delivery is asynchronous and retryable.
- Mapping failures are permanent failures for the sink outbox item only.

## Related Packages

- `Incursa.Platform.Audit`
- `Incursa.Integrations.WorkOS`
