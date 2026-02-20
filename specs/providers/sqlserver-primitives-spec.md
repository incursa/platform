# SQL Server Provider Primitive Specification

## Meta
- Provider: `Incursa.Platform.SqlServer`
- Status: Active
- Last Updated: 2026-02-20

## Scope
This spec defines SQL Server provider behavior for outbox, inbox, scheduler, and leases.
All scenarios are testable and traceable using `PRIM-SQLSERVER-*` identifiers.

## Outbox
- `PRIM-SQLSERVER-OUTBOX-001`: `ClaimAsync` with `batchSize <= 0` throws `ArgumentOutOfRangeException`.
- `PRIM-SQLSERVER-OUTBOX-002`: `ClaimAsync` returns at most `batchSize` ready items.
- `PRIM-SQLSERVER-OUTBOX-003`: `AckAsync` by non-owner does not acknowledge claimed rows.
- `PRIM-SQLSERVER-OUTBOX-004`: `AbandonAsync` by owner returns claimed rows to ready state with retry scheduling.
- `PRIM-SQLSERVER-OUTBOX-005`: `FailAsync` marks rows permanently failed and removes claim ownership.
- `PRIM-SQLSERVER-OUTBOX-006`: `ClaimDueAsync` excludes future-due rows.

## Inbox
- `PRIM-SQLSERVER-INBOX-001`: `AlreadyProcessedAsync` rejects null/whitespace `messageId` and `source`.
- `PRIM-SQLSERVER-INBOX-002`: `MarkProcessingAsync` transitions message state to processing.
- `PRIM-SQLSERVER-INBOX-003`: `MarkProcessedAsync` transitions message state to done.
- `PRIM-SQLSERVER-INBOX-004`: `MarkDeadAsync` transitions message state to dead.
- `PRIM-SQLSERVER-INBOX-005`: inbox work claims are owner-token enforced for ack/abandon/fail.

## Scheduler
- `PRIM-SQLSERVER-SCHED-001`: timers are not claimed before due time.
- `PRIM-SQLSERVER-SCHED-002`: `ClaimTimersAsync` honors batch size.
- `PRIM-SQLSERVER-SCHED-003`: `AckTimersAsync` by owner finalizes claimed timer rows.
- `PRIM-SQLSERVER-SCHED-004`: `AbandonTimersAsync` returns claimed timers to ready state.
- `PRIM-SQLSERVER-SCHED-005`: `CreateOrUpdateJobAsync` inserts or updates by job identity.

## Lease
- `PRIM-SQLSERVER-LEASE-001`: `AcquireAsync` succeeds on free resource and returns valid fencing token.
- `PRIM-SQLSERVER-LEASE-002`: `AcquireAsync` returns null when lease is held by a different owner.
- `PRIM-SQLSERVER-LEASE-003`: `TryRenewNowAsync` succeeds for valid owner and extends expiry.
- `PRIM-SQLSERVER-LEASE-004`: renew fails for invalid owner.

## Schema Deployment
- `PRIM-SQLSERVER-SCHEMA-001`: schema deployment is idempotent for repeated execution.
- `PRIM-SQLSERVER-SCHEMA-002`: custom schema names place artifacts in configured schema.
- `PRIM-SQLSERVER-SCHEMA-003`: required tables/indexes/procedures exist after deployment.
