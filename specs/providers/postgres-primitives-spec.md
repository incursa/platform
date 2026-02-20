# Postgres Provider Primitive Specification

## Meta
- Provider: `Incursa.Platform.Postgres`
- Status: Active
- Last Updated: 2026-02-20

## Scope
This spec defines Postgres provider behavior for outbox, inbox, scheduler, and leases.
All scenarios are testable and traceable using `PRIM-POSTGRES-*` identifiers.

## Outbox
- `PRIM-POSTGRES-OUTBOX-001`: `ClaimAsync` with `batchSize <= 0` throws `ArgumentOutOfRangeException`.
- `PRIM-POSTGRES-OUTBOX-002`: `ClaimAsync` returns at most `batchSize` ready items.
- `PRIM-POSTGRES-OUTBOX-003`: `AckAsync` by non-owner does not acknowledge claimed rows.
- `PRIM-POSTGRES-OUTBOX-004`: `AbandonAsync` by owner returns claimed rows to ready state with retry scheduling.
- `PRIM-POSTGRES-OUTBOX-005`: `FailAsync` marks rows permanently failed and removes claim ownership.
- `PRIM-POSTGRES-OUTBOX-006`: `ClaimDueAsync` excludes future-due rows.

## Inbox
- `PRIM-POSTGRES-INBOX-001`: `AlreadyProcessedAsync` rejects null/whitespace `messageId` and `source`.
- `PRIM-POSTGRES-INBOX-002`: `MarkProcessingAsync` transitions message state to processing.
- `PRIM-POSTGRES-INBOX-003`: `MarkProcessedAsync` transitions message state to done.
- `PRIM-POSTGRES-INBOX-004`: `MarkDeadAsync` transitions message state to dead.
- `PRIM-POSTGRES-INBOX-005`: inbox work claims are owner-token enforced for ack/abandon/fail.

## Scheduler
- `PRIM-POSTGRES-SCHED-001`: timers are not claimed before due time.
- `PRIM-POSTGRES-SCHED-002`: `ClaimTimersAsync` honors batch size.
- `PRIM-POSTGRES-SCHED-003`: `AckTimersAsync` by owner finalizes claimed timer rows.
- `PRIM-POSTGRES-SCHED-004`: `AbandonTimersAsync` returns claimed timers to ready state.
- `PRIM-POSTGRES-SCHED-005`: `CreateOrUpdateJobAsync` inserts or updates by job identity.

## Lease
- `PRIM-POSTGRES-LEASE-001`: `AcquireAsync` succeeds on free resource and returns valid fencing token.
- `PRIM-POSTGRES-LEASE-002`: `AcquireAsync` returns null when lease is held by a different owner.
- `PRIM-POSTGRES-LEASE-003`: `TryRenewNowAsync` succeeds for valid owner and extends expiry.
- `PRIM-POSTGRES-LEASE-004`: renew fails for invalid owner.

## Schema Deployment
- `PRIM-POSTGRES-SCHEMA-001`: schema deployment is idempotent for repeated execution.
- `PRIM-POSTGRES-SCHEMA-002`: custom schema names place artifacts in configured schema.
- `PRIM-POSTGRES-SCHEMA-003`: required tables/indexes/functions exist after deployment.
