# Sentry External Handoff: Incursa-Owned Issues (24h)

Date: 2026-03-04
Org: `payewaive`
Window: last 24 hours

## Scope
This document includes only issues where the culprit indicates external/shared ownership under:
- `Incursa.Platform`
- `Incursa.Platform.SqlServer`

Primary Sentry queries:
- `lastSeen:-24h culprit:*Incursa.Platform*`
- `lastSeen:-24h culprit:*Incursa.Platform.SqlServer*`

## Executive Summary
- External issues identified: **22 unique issue IDs**
- Dominant failure family: SQL lease/inbox/outbox/scheduler acquisition and connectivity failures
- Highest-impact IDs by volume:
  - `PAYEWAIVE-1PK` (219 events, 27 users)
  - `PAYEWAIVE-1PM` (184 events, 17 users)
  - `PAYEWAIVE-1Q3` (74 events, 6 users)
- One additional non-SQL external issue observed:
  - `PAYEWAIVE-1QG` (`Incursa.Platform/Outbox`, OOM in logging scope path)

## Issue Inventory (External)

### A) SQL lease/connectivity/transport/timeout cluster (`Incursa.Platform.SqlServer`)
Issue IDs:
- `PAYEWAIVE-1PK`, `PAYEWAIVE-1PM`
- `PAYEWAIVE-1Q3`, `PAYEWAIVE-1Q5`, `PAYEWAIVE-1Q6`, `PAYEWAIVE-1PR`, `PAYEWAIVE-1Q4`
- `PAYEWAIVE-1PY`, `PAYEWAIVE-1Q1`, `PAYEWAIVE-1Q2`, `PAYEWAIVE-1Q0`, `PAYEWAIVE-1PZ`, `PAYEWAIVE-1PX`
- `PAYEWAIVE-1PW`, `PAYEWAIVE-1PS`, `PAYEWAIVE-1PV`, `PAYEWAIVE-1PT`, `PAYEWAIVE-1PQ`, `PAYEWAIVE-1Q8`
- `PAYEWAIVE-1QA`, `PAYEWAIVE-1R0`

Representative culprits:
- `Incursa.Platform.SqlServer/Lease/SqlLease.AcquireAsync`
- `Incursa.Platform.SqlServer/Inbox/SqlInboxWorkStore.ClaimAsync`
- `Incursa.Platform.SqlServer/Outbox/SqlOutboxStore.ClaimDueAsync`
- `Incursa.Platform.SqlServer/Scheduler/SqlSchedulerStore.CreateJobRunsFromDueJobsAsync`

Observed error signatures:
- `Cannot open server ... Client IP is not allowed` (firewall/allowlist)
- `Execution Timeout Expired`
- `Operation cancelled by user`
- `A transport-level error has occurred ... TCP Provider, error: 35`
- `pre-login handshake failed ... SSL Provider, error: 31`

### B) Outbox OOM path (`Incursa.Platform`)
Issue ID:
- `PAYEWAIVE-1QG`

Representative culprit:
- `Incursa.Platform/Outbox/MultiOutboxDispatcher.ProcessStoreAsync`

Observed error signature:
- `System.AggregateException: An error occurred while writing to logger(s)`
- Inner: `System.OutOfMemoryException` in `Logger.BeginScope` / Sentry scope cloning path

## Evidence Links (Sentry)
- `PAYEWAIVE-1PK`: https://payewaive.sentry.io/issues/PAYEWAIVE-1PK
- `PAYEWAIVE-1PM`: https://payewaive.sentry.io/issues/PAYEWAIVE-1PM
- `PAYEWAIVE-1Q3`: https://payewaive.sentry.io/issues/PAYEWAIVE-1Q3
- `PAYEWAIVE-1QG`: https://payewaive.sentry.io/issues/PAYEWAIVE-1QG

Query links:
- https://payewaive.sentry.io/issues/?query=lastSeen%3A-24h+culprit%3A*Incursa.Platform*
- https://payewaive.sentry.io/issues/?query=lastSeen%3A-24h+culprit%3A*Incursa.Platform.SqlServer*

## Requested Actions for Incursa Team

### 1) SQL infrastructure resilience (P0)
- Triage and patch `SqlLease` acquisition behavior across cancellation/timeout/transport classes.
- Validate retry/backoff strategy and classification of expected cancellation vs actionable failures.
- Confirm handling strategy for firewall-denied and TLS pre-login failures.
- Reduce error-level noise for handled transient states where appropriate.

### 2) Inbox/outbox/scheduler store hardening (P0)
- Validate connection pool and transient fault handling in:
  - `SqlInboxWorkStore.ClaimAsync`
  - `SqlOutboxStore.ClaimDueAsync` / `RescheduleAsync`
  - `SqlSchedulerStore.CreateJobRunsFromDueJobsAsync`
- Add safeguards to avoid cascade amplification during partial SQL outages.

### 3) Outbox logging/OOM behavior (P1)
- Investigate memory amplification in logging scope creation (`Logger.BeginScope` / Sentry scope clone).
- Ensure outbox polling loop degrades gracefully under high memory pressure.
- Add backpressure or bounded telemetry payload for outbox iteration logs.

## Acceptance Criteria
- No new events for the 22 external IDs across 48 hours in production after patch deployment.
- SQL cluster error rate reduced to baseline, with no recurring firewall/handshake/transport bursts.
- No recurrence of `PAYEWAIVE-1QG` OOM path.

## Ownership Boundary Note
Application-level fixes already applied in our repo do not modify Incursa platform/sqlserver code paths. These 22 IDs require fixes in shared Incursa component code paths.
