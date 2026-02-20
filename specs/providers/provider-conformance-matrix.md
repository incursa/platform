# Provider Primitive Conformance Matrix

## Scope
Traceability matrix for provider primitive scenarios in:
- `specs/providers/sqlserver-primitives-spec.md`
- `specs/providers/postgres-primitives-spec.md`
- `specs/providers/inmemory-primitives-spec.md`

Status values:
- `Covered`: scenario is mapped to one or more automated tests.
- `Missing`: no mapped test exists yet.
- `Deferred`: intentionally deferred with rationale.

| Scenario ID | Provider | Area | Status | Mapped Test(s) |
| --- | --- | --- | --- | --- |
| PRIM-SQLSERVER-OUTBOX-002 | SqlServer | Outbox | Covered | `tests/Incursa.Platform.SqlServer.Tests/SqlOutboxStoreBehaviorTests.cs` |
| PRIM-SQLSERVER-OUTBOX-003 | SqlServer | Outbox | Covered | `tests/Incursa.Platform.SqlServer.Tests/OutboxWorkQueueTests.cs` |
| PRIM-SQLSERVER-INBOX-005 | SqlServer | Inbox | Covered | `tests/Incursa.Platform.SqlServer.Tests/SqlInboxWorkStoreBehaviorTests.cs` |
| PRIM-SQLSERVER-SCHED-002 | SqlServer | Scheduler | Covered | `tests/Incursa.Platform.SqlServer.Tests/SqlSchedulerClientTests.cs` |
| PRIM-SQLSERVER-LEASE-003 | SqlServer | Lease | Covered | `tests/Incursa.Platform.SqlServer.Tests/LeaseRunnerTests.cs` |
| PRIM-SQLSERVER-SCHEMA-003 | SqlServer | Schema | Covered | `tests/Incursa.Platform.SqlServer.Tests/DatabaseSchemaConsistencyTests.cs` |
| PRIM-POSTGRES-OUTBOX-002 | Postgres | Outbox | Covered | `tests/Incursa.Platform.Postgres.Tests/PostgresOutboxStoreBehaviorTests.cs` |
| PRIM-POSTGRES-INBOX-005 | Postgres | Inbox | Covered | `tests/Incursa.Platform.Postgres.Tests/PostgresInboxWorkStoreBehaviorTests.cs` |
| PRIM-POSTGRES-SCHED-002 | Postgres | Scheduler | Covered | `tests/Incursa.Platform.Postgres.Tests/PostgresSchedulerClientTests.cs` |
| PRIM-POSTGRES-LEASE-003 | Postgres | Lease | Covered | `tests/Incursa.Platform.Postgres.Tests/PostgresLeaseRunnerTests.cs` |
| PRIM-POSTGRES-SCHEMA-003 | Postgres | Schema | Covered | `tests/Incursa.Platform.Postgres.Tests/DatabaseSchemaConsistencyTests.cs` |
| PRIM-INMEMORY-GEN-001 | InMemory | General | Covered | `src/Incursa.Platform.InMemory/README.md` |
| PRIM-INMEMORY-GEN-002 | InMemory | General | Covered | `src/Incursa.Platform.InMemory/README.md` |
| PRIM-INMEMORY-GEN-003 | InMemory | General | Covered | `tests/Incursa.Platform.InMemory.Tests/InMemoryOutboxStoreBehaviorTests.cs`, `tests/Incursa.Platform.InMemory.Tests/InMemoryInboxWorkStoreBehaviorTests.cs` |
| PRIM-INMEMORY-OUTBOX-001 | InMemory | Outbox | Covered | `tests/Incursa.Platform.InMemory.Tests/InMemoryOutboxStoreBehaviorTests.cs` |
| PRIM-INMEMORY-OUTBOX-002 | InMemory | Outbox | Covered | `tests/Incursa.Platform.InMemory.Tests/InMemoryOutboxStoreBehaviorTests.cs` |
| PRIM-INMEMORY-OUTBOX-003 | InMemory | Outbox | Covered | `tests/Incursa.Platform.InMemory.Tests/InMemoryOutboxStoreBehaviorTests.cs` |
| PRIM-INMEMORY-OUTBOX-004 | InMemory | Outbox | Covered | `tests/Incursa.Platform.InMemory.Tests/InMemoryOutboxStoreBehaviorTests.cs` |
| PRIM-INMEMORY-OUTBOX-005 | InMemory | Outbox | Covered | `tests/Incursa.Platform.InMemory.Tests/InMemoryOutboxStoreBehaviorTests.cs` |
| PRIM-INMEMORY-OUTBOX-006 | InMemory | Outbox | Covered | `tests/Incursa.Platform.InMemory.Tests/InMemoryOutboxStoreBehaviorTests.cs` |
| PRIM-INMEMORY-OUTBOX-007 | InMemory | Outbox | Covered | `tests/Incursa.Platform.InMemory.Tests/InMemoryOutboxStoreBehaviorTests.cs` |
| PRIM-INMEMORY-OUTBOX-008 | InMemory | Outbox | Covered | `tests/Incursa.Platform.InMemory.Tests/InMemoryOutboxStoreBehaviorTests.cs` |
| PRIM-INMEMORY-OUTBOX-009 | InMemory | Outbox | Covered | `tests/Incursa.Platform.InMemory.Tests/InMemoryOutboxTimeBehaviorTests.cs` |
| PRIM-INMEMORY-INBOX-001 | InMemory | Inbox | Covered | `tests/Incursa.Platform.InMemory.Tests/InMemoryInboxWorkStoreBehaviorTests.cs` |
| PRIM-INMEMORY-INBOX-002 | InMemory | Inbox | Covered | `tests/Incursa.Platform.InMemory.Tests/InMemoryInboxWorkStoreBehaviorTests.cs` |
| PRIM-INMEMORY-INBOX-003 | InMemory | Inbox | Covered | `tests/Incursa.Platform.InMemory.Tests/InMemoryInboxWorkStoreBehaviorTests.cs` |
| PRIM-INMEMORY-INBOX-004 | InMemory | Inbox | Covered | `tests/Incursa.Platform.InMemory.Tests/InMemoryInboxWorkStoreBehaviorTests.cs` |
| PRIM-INMEMORY-INBOX-005 | InMemory | Inbox | Covered | `tests/Incursa.Platform.InMemory.Tests/InMemoryInboxWorkStoreBehaviorTests.cs` |
| PRIM-INMEMORY-INBOX-006 | InMemory | Inbox | Covered | `tests/Incursa.Platform.InMemory.Tests/InMemoryInboxWorkStoreBehaviorTests.cs` |
| PRIM-INMEMORY-INBOX-007 | InMemory | Inbox | Covered | `tests/Incursa.Platform.InMemory.Tests/InMemoryInboxWorkStoreBehaviorTests.cs` |
| PRIM-INMEMORY-INBOX-008 | InMemory | Inbox | Covered | `tests/Incursa.Platform.InMemory.Tests/InMemoryInboxWorkStoreBehaviorTests.cs` |
| PRIM-INMEMORY-INBOX-009 | InMemory | Inbox | Covered | `tests/Incursa.Platform.InMemory.Tests/InMemoryInboxTimeBehaviorTests.cs` |
| PRIM-INMEMORY-SCHED-001 | InMemory | Scheduler | Covered | `tests/Incursa.Platform.InMemory.Tests/InMemorySchedulerBehaviorTests.cs` |
| PRIM-INMEMORY-SCHED-002 | InMemory | Scheduler | Covered | `tests/Incursa.Platform.InMemory.Tests/InMemorySchedulerBehaviorTests.cs` |
| PRIM-INMEMORY-SCHED-003 | InMemory | Scheduler | Covered | `tests/Incursa.Platform.InMemory.Tests/InMemorySchedulerBehaviorTests.cs` |
| PRIM-INMEMORY-SCHED-004 | InMemory | Scheduler | Covered | `tests/Incursa.Platform.InMemory.Tests/InMemorySchedulerBehaviorTests.cs` |
| PRIM-INMEMORY-SCHED-005 | InMemory | Scheduler | Covered | `tests/Incursa.Platform.InMemory.Tests/InMemorySchedulerBehaviorTests.cs` |
| PRIM-INMEMORY-SCHED-006 | InMemory | Scheduler | Covered | `tests/Incursa.Platform.InMemory.Tests/InMemorySchedulerBehaviorTests.cs` |
| PRIM-INMEMORY-SCHED-007 | InMemory | Scheduler | Covered | `tests/Incursa.Platform.InMemory.Tests/InMemorySchedulerBehaviorTests.cs` |
| PRIM-INMEMORY-SCHED-008 | InMemory | Scheduler | Covered | `tests/Incursa.Platform.InMemory.Tests/InMemorySchedulerBehaviorTests.cs` |
| PRIM-INMEMORY-SCHED-009 | InMemory | Scheduler | Covered | `tests/Incursa.Platform.InMemory.Tests/InMemorySchedulerBehaviorTests.cs` |
| PRIM-INMEMORY-SCHED-010 | InMemory | Scheduler | Covered | `tests/Incursa.Platform.InMemory.Tests/InMemorySchedulerBehaviorTests.cs` |
| PRIM-INMEMORY-LEASE-001 | InMemory | Lease | Covered | `tests/Incursa.Platform.InMemory.Tests/InMemorySystemLeaseBehaviorTests.cs` |
| PRIM-INMEMORY-LEASE-002 | InMemory | Lease | Covered | `tests/Incursa.Platform.InMemory.Tests/InMemorySystemLeaseBehaviorTests.cs` |
| PRIM-INMEMORY-LEASE-003 | InMemory | Lease | Covered | `tests/Incursa.Platform.InMemory.Tests/InMemorySystemLeaseBehaviorTests.cs` |
| PRIM-INMEMORY-LEASE-004 | InMemory | Lease | Covered | `tests/Incursa.Platform.InMemory.Tests/InMemorySystemLeaseBehaviorTests.cs` |
| PRIM-INMEMORY-LEASE-005 | InMemory | Lease | Covered | `tests/Incursa.Platform.InMemory.Tests/InMemorySystemLeaseBehaviorTests.cs` |
| PRIM-INMEMORY-LEASE-006 | InMemory | Lease | Covered | `tests/Incursa.Platform.InMemory.Tests/InMemorySystemLeaseBehaviorTests.cs` |
| PRIM-INMEMORY-LEASE-007 | InMemory | Lease | Covered | `tests/Incursa.Platform.InMemory.Tests/InMemorySystemLeaseBehaviorTests.cs` |
| PRIM-INMEMORY-LEASE-008 | InMemory | Lease | Covered | `tests/Incursa.Platform.InMemory.Tests/InMemorySystemLeaseBehaviorTests.cs` |
| PRIM-INMEMORY-LEASE-009 | InMemory | Lease | Covered | `tests/Incursa.Platform.InMemory.Tests/InMemorySystemLeaseBehaviorTests.cs` |
| PRIM-INMEMORY-LEASE-010 | InMemory | Lease | Covered | `tests/Incursa.Platform.InMemory.Tests/InMemorySystemLeaseBehaviorTests.cs` |

## Next Updates
- Port scheduler and lease harnesses to SqlServer/Postgres provider suites.
- Replace temporary General scenario mapping to README with executable behavioral tests where practical.
