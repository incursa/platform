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
| PRIM-INMEMORY-OUTBOX-001 | InMemory | Outbox | Covered | `tests/Incursa.Platform.InMemory.Tests/InMemoryOutboxStoreBehaviorTests.cs` |
| PRIM-INMEMORY-INBOX-001 | InMemory | Inbox | Covered | `tests/Incursa.Platform.InMemory.Tests/InMemoryInboxWorkStoreBehaviorTests.cs` |
| PRIM-INMEMORY-SCHED-001 | InMemory | Scheduler | Missing | - |
| PRIM-INMEMORY-LEASE-002 | InMemory | Lease | Missing | - |

## Next Updates
- Fill missing `InMemory` scheduler and lease conformance tests.
- Expand matrix rows until all scenarios in provider specs are represented.
