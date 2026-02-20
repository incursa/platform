# InMemory Portability Checklist

## Purpose
Use the in-memory primitive conformance suite as the portability baseline for SQL-backed providers.

## Source of Truth
- Spec: `specs/providers/inmemory-primitives-spec.md`
- Traceability: `specs/providers/provider-conformance-matrix.md`
- Shared harnesses:
  - `tests/Incursa.Platform.TestUtilities/SchedulerBehaviorTestsBase.cs`
  - `tests/Incursa.Platform.TestUtilities/SystemLeaseBehaviorTestsBase.cs`

## Porting Targets

### Scheduler
- Claim due timers behavior and batch-size semantics.
- Timer ack/abandon/reap transitions.
- Job trigger and claim semantics.
- Job upsert/update semantics.
- Job delete and pending-run cleanup semantics.
- Due-job materialization (`CreateJobRunsFromDueJobsAsync`) semantics.
- `GetNextEventTimeAsync` behavior.

### Lease
- Acquire free/occupied semantics.
- Owner token preservation.
- Renew success/failure semantics.
- Fencing token progression.
- Lost lease detection (`ThrowIfLost`) behavior.
- Loss cancellation token behavior.
- Dispose/reacquisition semantics.

## Execution Order
1. Port shared scheduler harness coverage to `SqlServer` tests. Status: Completed.
2. Port shared lease harness coverage to `SqlServer` tests. Status: Completed.
3. Port shared scheduler harness coverage to `Postgres` tests. Status: Pending.
4. Port shared lease harness coverage to `Postgres` tests. Status: Pending.
5. Reconcile provider-specific differences as explicit deferred scenarios.
