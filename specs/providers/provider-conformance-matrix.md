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
| PRIM-SQLSERVER-OUTBOX-001 | SqlServer | Outbox | Covered | `tests/Incursa.Platform.SqlServer.Tests/OutboxWorkQueueTests.cs` |
| PRIM-SQLSERVER-OUTBOX-002 | SqlServer | Outbox | Covered | `tests/Incursa.Platform.SqlServer.Tests/SqlOutboxStoreBehaviorTests.cs` |
| PRIM-SQLSERVER-OUTBOX-003 | SqlServer | Outbox | Covered | `tests/Incursa.Platform.SqlServer.Tests/OutboxWorkQueueTests.cs` |
| PRIM-SQLSERVER-OUTBOX-004 | SqlServer | Outbox | Covered | `tests/Incursa.Platform.SqlServer.Tests/OutboxWorkQueueTests.cs` |
| PRIM-SQLSERVER-OUTBOX-005 | SqlServer | Outbox | Covered | `tests/Incursa.Platform.SqlServer.Tests/OutboxWorkQueueTests.cs` |
| PRIM-SQLSERVER-OUTBOX-006 | SqlServer | Outbox | Covered | `tests/Incursa.Platform.SqlServer.Tests/SqlOutboxStoreBehaviorTests.cs` |
| PRIM-SQLSERVER-INBOX-001 | SqlServer | Inbox | Covered | `tests/Incursa.Platform.SqlServer.Tests/SqlInboxServiceTests.cs` |
| PRIM-SQLSERVER-INBOX-002 | SqlServer | Inbox | Covered | `tests/Incursa.Platform.SqlServer.Tests/SqlInboxServiceTests.cs` |
| PRIM-SQLSERVER-INBOX-003 | SqlServer | Inbox | Covered | `tests/Incursa.Platform.SqlServer.Tests/SqlInboxServiceTests.cs` |
| PRIM-SQLSERVER-INBOX-004 | SqlServer | Inbox | Covered | `tests/Incursa.Platform.SqlServer.Tests/SqlInboxServiceTests.cs` |
| PRIM-SQLSERVER-INBOX-005 | SqlServer | Inbox | Covered | `tests/Incursa.Platform.SqlServer.Tests/SqlInboxWorkStoreBehaviorTests.cs` |
| PRIM-SQLSERVER-SCHED-002 | SqlServer | Scheduler | Covered | `tests/Incursa.Platform.SqlServer.Tests/SqlSchedulerClientTests.cs` |
| PRIM-SQLSERVER-LEASE-003 | SqlServer | Lease | Covered | `tests/Incursa.Platform.SqlServer.Tests/LeaseRunnerTests.cs` |
| PRIM-SQLSERVER-SCHEMA-001 | SqlServer | Schema | Covered | `tests/Incursa.Platform.SqlServer.Tests/DatabaseSchemaConsistencyTests.cs` |
| PRIM-SQLSERVER-SCHEMA-002 | SqlServer | Schema | Covered | `tests/Incursa.Platform.SqlServer.Tests/DatabaseSchemaConsistencyTests.cs` |
| PRIM-SQLSERVER-SCHEMA-003 | SqlServer | Schema | Covered | `tests/Incursa.Platform.SqlServer.Tests/DatabaseSchemaConsistencyTests.cs` |
| PRIM-SQLSERVER-API-001 | SqlServer | PublicApi | Covered | `tests/Incursa.Platform.SqlServer.Tests/SqlPlatformRegistrationTests.cs` |
| PRIM-SQLSERVER-API-002 | SqlServer | PublicApi | Covered | `tests/Incursa.Platform.SqlServer.Tests/PlatformRegistrationTests.cs` |
| PRIM-SQLSERVER-API-003 | SqlServer | PublicApi | Covered | `tests/Incursa.Platform.SqlServer.Tests/OptionsValidationTests.cs` |
| PRIM-SQLSERVER-FUZZ-001 | SqlServer | Fuzz | Covered | `tests/Incursa.Platform.SqlServer.Tests/OutboxWorkQueueTests.cs` |
| PRIM-SQLSERVER-SCHED-001 | SqlServer | Scheduler | Covered | `tests/Incursa.Platform.SqlServer.Tests/SqlSchedulerBehaviorTests.cs` |
| PRIM-SQLSERVER-SCHED-003 | SqlServer | Scheduler | Covered | `tests/Incursa.Platform.SqlServer.Tests/SqlSchedulerBehaviorTests.cs` |
| PRIM-SQLSERVER-SCHED-004 | SqlServer | Scheduler | Covered | `tests/Incursa.Platform.SqlServer.Tests/SqlSchedulerBehaviorTests.cs` |
| PRIM-SQLSERVER-SCHED-005 | SqlServer | Scheduler | Covered | `tests/Incursa.Platform.SqlServer.Tests/SqlSchedulerClientTests.cs` |
| PRIM-SQLSERVER-SCHED-006 | SqlServer | Scheduler | Covered | `tests/Incursa.Platform.SqlServer.Tests/SqlSchedulerBehaviorTests.cs` |
| PRIM-SQLSERVER-SCHED-007 | SqlServer | Scheduler | Covered | `tests/Incursa.Platform.SqlServer.Tests/SqlSchedulerBehaviorTests.cs` |
| PRIM-SQLSERVER-SCHED-008 | SqlServer | Scheduler | Covered | `tests/Incursa.Platform.SqlServer.Tests/SqlSchedulerBehaviorTests.cs` |
| PRIM-SQLSERVER-SCHED-009 | SqlServer | Scheduler | Covered | `tests/Incursa.Platform.SqlServer.Tests/SqlSchedulerBehaviorTests.cs` |
| PRIM-SQLSERVER-SCHED-010 | SqlServer | Scheduler | Covered | `tests/Incursa.Platform.SqlServer.Tests/SqlSchedulerBehaviorTests.cs` |
| PRIM-SQLSERVER-LEASE-001 | SqlServer | Lease | Covered | `tests/Incursa.Platform.SqlServer.Tests/SqlSystemLeaseBehaviorTests.cs` |
| PRIM-SQLSERVER-LEASE-002 | SqlServer | Lease | Covered | `tests/Incursa.Platform.SqlServer.Tests/SqlSystemLeaseBehaviorTests.cs` |
| PRIM-SQLSERVER-LEASE-004 | SqlServer | Lease | Covered | `tests/Incursa.Platform.SqlServer.Tests/SqlSystemLeaseBehaviorTests.cs` |
| PRIM-SQLSERVER-LEASE-005 | SqlServer | Lease | Covered | `tests/Incursa.Platform.SqlServer.Tests/SqlSystemLeaseBehaviorTests.cs` |
| PRIM-SQLSERVER-LEASE-006 | SqlServer | Lease | Covered | `tests/Incursa.Platform.SqlServer.Tests/SqlSystemLeaseBehaviorTests.cs` |
| PRIM-SQLSERVER-LEASE-007 | SqlServer | Lease | Covered | `tests/Incursa.Platform.SqlServer.Tests/SqlSystemLeaseBehaviorTests.cs` |
| PRIM-SQLSERVER-LEASE-008 | SqlServer | Lease | Covered | `tests/Incursa.Platform.SqlServer.Tests/SqlSystemLeaseBehaviorTests.cs` |
| PRIM-SQLSERVER-LEASE-009 | SqlServer | Lease | Covered | `tests/Incursa.Platform.SqlServer.Tests/SqlSystemLeaseBehaviorTests.cs` |
| PRIM-SQLSERVER-LEASE-010 | SqlServer | Lease | Covered | `tests/Incursa.Platform.SqlServer.Tests/SqlSystemLeaseBehaviorTests.cs` |
| PRIM-POSTGRES-OUTBOX-001 | Postgres | Outbox | Covered | `tests/Incursa.Platform.Postgres.Tests/OutboxWorkQueueTests.cs` |
| PRIM-POSTGRES-OUTBOX-002 | Postgres | Outbox | Covered | `tests/Incursa.Platform.Postgres.Tests/PostgresOutboxStoreBehaviorTests.cs` |
| PRIM-POSTGRES-OUTBOX-003 | Postgres | Outbox | Covered | `tests/Incursa.Platform.Postgres.Tests/OutboxWorkQueueTests.cs` |
| PRIM-POSTGRES-OUTBOX-004 | Postgres | Outbox | Covered | `tests/Incursa.Platform.Postgres.Tests/OutboxWorkQueueTests.cs` |
| PRIM-POSTGRES-OUTBOX-005 | Postgres | Outbox | Covered | `tests/Incursa.Platform.Postgres.Tests/OutboxWorkQueueTests.cs` |
| PRIM-POSTGRES-OUTBOX-006 | Postgres | Outbox | Covered | `tests/Incursa.Platform.Postgres.Tests/PostgresOutboxStoreBehaviorTests.cs` |
| PRIM-POSTGRES-INBOX-001 | Postgres | Inbox | Covered | `tests/Incursa.Platform.Postgres.Tests/PostgresInboxServiceTests.cs` |
| PRIM-POSTGRES-INBOX-002 | Postgres | Inbox | Covered | `tests/Incursa.Platform.Postgres.Tests/PostgresInboxServiceTests.cs` |
| PRIM-POSTGRES-INBOX-003 | Postgres | Inbox | Covered | `tests/Incursa.Platform.Postgres.Tests/PostgresInboxServiceTests.cs` |
| PRIM-POSTGRES-INBOX-004 | Postgres | Inbox | Covered | `tests/Incursa.Platform.Postgres.Tests/PostgresInboxServiceTests.cs` |
| PRIM-POSTGRES-INBOX-005 | Postgres | Inbox | Covered | `tests/Incursa.Platform.Postgres.Tests/PostgresInboxWorkStoreBehaviorTests.cs` |
| PRIM-POSTGRES-SCHED-001 | Postgres | Scheduler | Covered | `tests/Incursa.Platform.Postgres.Tests/PostgresSchedulerBehaviorTests.cs` |
| PRIM-POSTGRES-SCHED-002 | Postgres | Scheduler | Covered | `tests/Incursa.Platform.Postgres.Tests/PostgresSchedulerClientTests.cs` |
| PRIM-POSTGRES-SCHED-003 | Postgres | Scheduler | Covered | `tests/Incursa.Platform.Postgres.Tests/PostgresSchedulerBehaviorTests.cs` |
| PRIM-POSTGRES-SCHED-004 | Postgres | Scheduler | Covered | `tests/Incursa.Platform.Postgres.Tests/PostgresSchedulerBehaviorTests.cs` |
| PRIM-POSTGRES-SCHED-005 | Postgres | Scheduler | Covered | `tests/Incursa.Platform.Postgres.Tests/PostgresSchedulerClientTests.cs` |
| PRIM-POSTGRES-SCHED-006 | Postgres | Scheduler | Covered | `tests/Incursa.Platform.Postgres.Tests/PostgresSchedulerBehaviorTests.cs` |
| PRIM-POSTGRES-SCHED-007 | Postgres | Scheduler | Covered | `tests/Incursa.Platform.Postgres.Tests/PostgresSchedulerBehaviorTests.cs` |
| PRIM-POSTGRES-SCHED-008 | Postgres | Scheduler | Covered | `tests/Incursa.Platform.Postgres.Tests/PostgresSchedulerBehaviorTests.cs` |
| PRIM-POSTGRES-SCHED-009 | Postgres | Scheduler | Covered | `tests/Incursa.Platform.Postgres.Tests/PostgresSchedulerBehaviorTests.cs` |
| PRIM-POSTGRES-SCHED-010 | Postgres | Scheduler | Covered | `tests/Incursa.Platform.Postgres.Tests/PostgresSchedulerBehaviorTests.cs` |
| PRIM-POSTGRES-LEASE-001 | Postgres | Lease | Covered | `tests/Incursa.Platform.Postgres.Tests/LeaseTests.cs` |
| PRIM-POSTGRES-LEASE-002 | Postgres | Lease | Covered | `tests/Incursa.Platform.Postgres.Tests/LeaseTests.cs` |
| PRIM-POSTGRES-LEASE-003 | Postgres | Lease | Covered | `tests/Incursa.Platform.Postgres.Tests/PostgresLeaseRunnerTests.cs` |
| PRIM-POSTGRES-LEASE-004 | Postgres | Lease | Covered | `tests/Incursa.Platform.Postgres.Tests/LeaseTests.cs` |
| PRIM-POSTGRES-LEASE-005 | Postgres | Lease | Covered | `tests/Incursa.Platform.Postgres.Tests/PostgresSystemLeaseBehaviorTests.cs` |
| PRIM-POSTGRES-LEASE-006 | Postgres | Lease | Covered | `tests/Incursa.Platform.Postgres.Tests/PostgresSystemLeaseBehaviorTests.cs` |
| PRIM-POSTGRES-LEASE-007 | Postgres | Lease | Covered | `tests/Incursa.Platform.Postgres.Tests/PostgresSystemLeaseBehaviorTests.cs` |
| PRIM-POSTGRES-LEASE-008 | Postgres | Lease | Covered | `tests/Incursa.Platform.Postgres.Tests/PostgresSystemLeaseBehaviorTests.cs` |
| PRIM-POSTGRES-LEASE-009 | Postgres | Lease | Covered | `tests/Incursa.Platform.Postgres.Tests/PostgresSystemLeaseBehaviorTests.cs` |
| PRIM-POSTGRES-LEASE-010 | Postgres | Lease | Covered | `tests/Incursa.Platform.Postgres.Tests/PostgresSystemLeaseBehaviorTests.cs` |
| PRIM-POSTGRES-SCHEMA-001 | Postgres | Schema | Covered | `tests/Incursa.Platform.Postgres.Tests/DatabaseSchemaConsistencyTests.cs` |
| PRIM-POSTGRES-SCHEMA-002 | Postgres | Schema | Covered | `tests/Incursa.Platform.Postgres.Tests/CustomSchemaIntegrationTests.cs` |
| PRIM-POSTGRES-SCHEMA-003 | Postgres | Schema | Covered | `tests/Incursa.Platform.Postgres.Tests/DatabaseSchemaConsistencyTests.cs` |
| PRIM-POSTGRES-API-001 | Postgres | PublicApi | Covered | `tests/Incursa.Platform.Postgres.Tests/PostgresPublicApiContractTests.cs` |
| PRIM-POSTGRES-API-002 | Postgres | PublicApi | Covered | `tests/Incursa.Platform.Postgres.Tests/PostgresPublicApiContractTests.cs` |
| PRIM-POSTGRES-API-003 | Postgres | PublicApi | Covered | `tests/Incursa.Platform.Postgres.Tests/PostgresPublicApiContractTests.cs` |
| PRIM-POSTGRES-FUZZ-001 | Postgres | Fuzz | Covered | `tests/Incursa.Platform.Postgres.Tests/OutboxWorkQueueTests.cs` |
| PRIM-INMEMORY-GEN-001 | InMemory | General | Covered | `tests/Incursa.Platform.InMemory.Tests/InMemoryGeneralCharacteristicsTests.cs` |
| PRIM-INMEMORY-GEN-002 | InMemory | General | Covered | `tests/Incursa.Platform.InMemory.Tests/InMemoryGeneralCharacteristicsTests.cs` |
| PRIM-INMEMORY-GEN-003 | InMemory | General | Covered | `tests/Incursa.Platform.InMemory.Tests/InMemoryOutboxStoreBehaviorTests.cs`, `tests/Incursa.Platform.InMemory.Tests/InMemoryInboxWorkStoreBehaviorTests.cs` |
| PRIM-INMEMORY-API-001 | InMemory | PublicApi | Covered | `tests/Incursa.Platform.InMemory.Tests/InMemoryPublicApiContractTests.cs` |
| PRIM-INMEMORY-API-002 | InMemory | PublicApi | Covered | `tests/Incursa.Platform.InMemory.Tests/InMemoryPublicApiContractTests.cs` |
| PRIM-INMEMORY-API-003 | InMemory | PublicApi | Covered | `tests/Incursa.Platform.InMemory.Tests/InMemoryPublicApiContractTests.cs` |
| PRIM-INMEMORY-API-004 | InMemory | PublicApi | Covered | `tests/Incursa.Platform.InMemory.Tests/InMemoryPublicApiContractTests.cs` |
| PRIM-INMEMORY-API-005 | InMemory | PublicApi | Covered | `tests/Incursa.Platform.InMemory.Tests/InMemoryPublicApiContractTests.cs` |
| PRIM-INMEMORY-FUZZ-001 | InMemory | Fuzz | Covered | `tests/Incursa.Platform.InMemory.Tests/InMemoryOutboxFuzzTests.cs` |
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
- Raise provider coverage and mutation thresholds after two consecutive stable CI cycles.
