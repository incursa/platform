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
- `PRIM-POSTGRES-SCHED-006`: `TriggerJobAsync` creates claimable job runs.
- `PRIM-POSTGRES-SCHED-007`: updating an existing job identity changes topic/payload used by subsequent runs.
- `PRIM-POSTGRES-SCHED-008`: `DeleteJobAsync` removes pending runs for the deleted job.
- `PRIM-POSTGRES-SCHED-009`: `CreateJobRunsFromDueJobsAsync` materializes runs from due cron schedules.
- `PRIM-POSTGRES-SCHED-010`: `GetNextEventTimeAsync` returns the nearest due event when scheduled work exists.

## Lease
- `PRIM-POSTGRES-LEASE-001`: `AcquireAsync` succeeds on free resource and returns valid fencing token.
- `PRIM-POSTGRES-LEASE-002`: `AcquireAsync` returns null when lease is held by a different owner.
- `PRIM-POSTGRES-LEASE-003`: `TryRenewNowAsync` succeeds for valid owner and extends expiry.
- `PRIM-POSTGRES-LEASE-004`: renew fails for invalid owner.
- `PRIM-POSTGRES-LEASE-005`: successful renew increments fencing token.
- `PRIM-POSTGRES-LEASE-006`: lease loss after expiry cancels the lease cancellation token.
- `PRIM-POSTGRES-LEASE-007`: `ThrowIfLost` throws `LostLeaseException` after lease loss.
- `PRIM-POSTGRES-LEASE-008`: `TryRenewNowAsync` returns false after lease is lost.
- `PRIM-POSTGRES-LEASE-009`: disposing a lease releases ownership and allows reacquisition.
- `PRIM-POSTGRES-LEASE-010`: expired lease can be reacquired by a new lease instance.

## Schema Deployment
- `PRIM-POSTGRES-SCHEMA-001`: schema deployment is idempotent for repeated execution.
- `PRIM-POSTGRES-SCHEMA-002`: custom schema names place artifacts in configured schema.
- `PRIM-POSTGRES-SCHEMA-003`: required tables/indexes/functions exist after deployment.

## Public API Requirements
- `PRIM-POSTGRES-API-001`: `AddPostgresPlatform` registers core platform provider services (`IOutboxStoreProvider`, `IInboxWorkStoreProvider`, `ISchedulerStoreProvider`) and platform discovery.
- `PRIM-POSTGRES-API-002`: list-based multi-database registration rejects duplicate/invalid registration state and validates discovery contract setup.
- `PRIM-POSTGRES-API-003`: options validation enforces required Postgres option contracts (connection strings, cleanup interval bounds, schema requirements).

## Fuzz Invariants
- `PRIM-POSTGRES-FUZZ-001`: deterministic randomized outbox claim/ack/abandon/fail sequences preserve terminal-state safety (failed/acknowledged items are never reclaimed).
