# InMemory Provider Primitive Specification

## Meta
- Provider: `Incursa.Platform.InMemory`
- Status: Active
- Last Updated: 2026-02-20

## Scope
This spec defines in-memory provider behavior for outbox, inbox, scheduler, and leases.
All scenarios are testable and traceable using `PRIM-INMEMORY-*` identifiers.

## Intended Use
- `PRIM-INMEMORY-GEN-001`: provider is process-local and non-durable.
- `PRIM-INMEMORY-GEN-002`: provider is for test and local development, not production coordination.

## Outbox
- `PRIM-INMEMORY-OUTBOX-001`: `ClaimDueAsync` excludes future-due rows.
- `PRIM-INMEMORY-OUTBOX-002`: `MarkDispatchedAsync` removes item from claimed set.
- `PRIM-INMEMORY-OUTBOX-003`: `RescheduleAsync` returns item to a future due state.
- `PRIM-INMEMORY-OUTBOX-004`: `FailAsync` marks item permanently failed.

## Inbox
- `PRIM-INMEMORY-INBOX-001`: claims are owner-token enforced for ack/abandon/fail.
- `PRIM-INMEMORY-INBOX-002`: `AbandonAsync` returns message to queue.
- `PRIM-INMEMORY-INBOX-003`: `AckAsync` finalizes message state.
- `PRIM-INMEMORY-INBOX-004`: `FailAsync` finalizes dead state.

## Scheduler
- `PRIM-INMEMORY-SCHED-001`: due timers are claimable only when due.
- `PRIM-INMEMORY-SCHED-002`: ack/abandon semantics mirror core scheduler contracts.

## Lease
- `PRIM-INMEMORY-LEASE-001`: acquire succeeds when resource is free.
- `PRIM-INMEMORY-LEASE-002`: renew requires valid owner context.
- `PRIM-INMEMORY-LEASE-003`: released/expired leases become acquirable again.
