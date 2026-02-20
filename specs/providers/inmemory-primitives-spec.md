# InMemory Provider Primitive Specification

## Meta
- Provider: `Incursa.Platform.InMemory`
- Status: Active
- Last Updated: 2026-02-20
- Scope Owner: Platform team

## Purpose and Scope
This specification defines behavior for the in-memory provider implementation of:
- Outbox
- Inbox
- Scheduler
- System leases

The in-memory provider is the fast conformance reference used to validate primitive semantics prior to SQL provider hardening.
All requirements use stable scenario IDs (`PRIM-INMEMORY-*`) and are traceable to automated tests.

## General Characteristics
- `PRIM-INMEMORY-GEN-001`: State is process-local and non-durable.
- `PRIM-INMEMORY-GEN-002`: The provider is intended for local development and test execution, not production distributed coordination.
- `PRIM-INMEMORY-GEN-003`: Primitive APIs preserve owner-token safety rules for claim/ack/abandon/fail operations.

## Outbox Requirements
- `PRIM-INMEMORY-OUTBOX-001`: `ClaimDueAsync` with no data returns an empty list.
- `PRIM-INMEMORY-OUTBOX-002`: due messages are claimable and returned with topic/payload.
- `PRIM-INMEMORY-OUTBOX-003`: future-due messages are not claimable until due.
- `PRIM-INMEMORY-OUTBOX-004`: claimed messages include persisted `CorrelationId` and `DueTimeUtc`.
- `PRIM-INMEMORY-OUTBOX-005`: `MarkDispatchedAsync` finalizes claimed messages and removes them from future claims.
- `PRIM-INMEMORY-OUTBOX-006`: `RescheduleAsync` returns messages to ready state and increments `RetryCount`.
- `PRIM-INMEMORY-OUTBOX-007`: `RescheduleAsync` stores `LastError`.
- `PRIM-INMEMORY-OUTBOX-008`: `FailAsync` marks message as failed and unavailable for claim.
- `PRIM-INMEMORY-OUTBOX-009`: claim eligibility is based on provider time source.

## Inbox Requirements
- `PRIM-INMEMORY-INBOX-001`: `ClaimAsync` with no messages returns empty.
- `PRIM-INMEMORY-INBOX-002`: claim returns available ready message identifiers.
- `PRIM-INMEMORY-INBOX-003`: future-due messages are not claimable until due.
- `PRIM-INMEMORY-INBOX-004`: `AckAsync` by owner finalizes claim and prevents reclain.
- `PRIM-INMEMORY-INBOX-005`: `AbandonAsync` by owner returns message to ready state.
- `PRIM-INMEMORY-INBOX-006`: `FailAsync` by owner transitions message to dead/unavailable.
- `PRIM-INMEMORY-INBOX-007`: `GetAsync` returns stored message payload metadata.
- `PRIM-INMEMORY-INBOX-008`: `ReviveAsync` requeues previously dead messages.
- `PRIM-INMEMORY-INBOX-009`: claim eligibility is based on provider time source.

## Scheduler Requirements
- `PRIM-INMEMORY-SCHED-001`: due timers can be claimed through `ClaimTimersAsync`.
- `PRIM-INMEMORY-SCHED-002`: timer claim respects `batchSize`.
- `PRIM-INMEMORY-SCHED-003`: `AckTimersAsync` finalizes claimed timers.
- `PRIM-INMEMORY-SCHED-004`: `AbandonTimersAsync` returns timers to ready for reclaim.
- `PRIM-INMEMORY-SCHED-005`: `ReapExpiredTimersAsync` returns expired in-progress timers to ready.
- `PRIM-INMEMORY-SCHED-006`: `TriggerJobAsync` creates claimable job runs.
- `PRIM-INMEMORY-SCHED-007`: `CreateOrUpdateJobAsync` updates existing job definition used by subsequent runs.
- `PRIM-INMEMORY-SCHED-008`: `DeleteJobAsync` removes pending runs for deleted job.
- `PRIM-INMEMORY-SCHED-009`: `CreateJobRunsFromDueJobsAsync` materializes runs from due cron schedules.
- `PRIM-INMEMORY-SCHED-010`: `GetNextEventTimeAsync` returns next due event when work exists.

## Lease Requirements
- `PRIM-INMEMORY-LEASE-001`: `AcquireAsync` succeeds for free resource.
- `PRIM-INMEMORY-LEASE-002`: `AcquireAsync` honors caller-provided owner token.
- `PRIM-INMEMORY-LEASE-003`: `AcquireAsync` returns null when lease is held and unexpired.
- `PRIM-INMEMORY-LEASE-004`: `TryRenewNowAsync` succeeds for valid current lease owner.
- `PRIM-INMEMORY-LEASE-005`: successful renewal increments fencing token.
- `PRIM-INMEMORY-LEASE-006`: after lease loss/expiry, lease cancellation token is canceled.
- `PRIM-INMEMORY-LEASE-007`: `ThrowIfLost` throws `LostLeaseException` after loss.
- `PRIM-INMEMORY-LEASE-008`: `TryRenewNowAsync` returns false after lease is lost.
- `PRIM-INMEMORY-LEASE-009`: disposing a lease releases ownership and allows reacquisition.
- `PRIM-INMEMORY-LEASE-010`: expired lease can be reacquired by a new lease instance.
