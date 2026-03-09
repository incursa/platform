# Proof Healing Behavior Specification

This document defines expected behavior for background healing polling and persistence-backed recovery.

## Scenarios

- `PRF-HEAL-001`: When healing is disabled, polling cycle processes zero transactions.
- `PRF-HEAL-002`: When healing is enabled, polling processes distinct due IDs and notifies observers.
- `PRF-HEAL-003`: SQL claim semantics use lease-based concurrency controls (`READPAST/UPDLOCK` for SQL Server, `SKIP LOCKED` for Postgres).
- `PRF-HEAL-004`: Polling failure records error and schedules next poll using configured retry delay.
- `PRF-HEAL-005`: After configured consecutive failures, transaction is quarantined for configured duration.
- `PRF-HEAL-006`: Quarantined rows are excluded from due-claim selection until quarantine expiry.
- `PRF-HEAL-007`: Terminal webhook/status handling clears lease/next poll and marks completion timestamp.
- `PRF-HEAL-008`: Startup schema migration is idempotent and protected by a migration lock timeout.
- `PRF-HEAL-009`: Successful/failure polling intervals are jittered to reduce synchronized spikes.
- `PRF-HEAL-010`: If no `IProofClient` is registered while healing is enabled, cycle logs and safely skips.
