# Database provider architecture

## Boundary

Provider-neutral contracts and orchestration belong in `platform`. Concrete database implementations, SQL dialects, driver dependencies, schema artifacts, and provider registration belong in provider packages.

This direction keeps application contracts independent of SQL Server, PostgreSQL, SQLite, or a particular migration engine while allowing each provider to document and test its own guarantees.

## Provider responsibilities

A database provider owns:

- connection and transaction integration;
- SQL dialect, locking, leasing, and concurrency behavior;
- schema and migration artifacts;
- provider-specific dependency injection;
- retry, timeout, and failure classification;
- drift, integrity, backup, and recovery guidance; and
- focused tests for the contracts it claims to implement.

The provider-neutral layer owns public interfaces, identifiers, routing, orchestration, lifecycle abstractions, and behavior that does not require a database dialect.

## SQL Server mapping

The SQL Server provider owns T-SQL, stored procedures, SQL Server types, locking hints, application locks, table-valued parameters, identity and row-version behavior, and SQL Server schema deployment.

## PostgreSQL mapping

A PostgreSQL provider must express equivalent public behavior with PostgreSQL-native primitives such as `ON CONFLICT`, `RETURNING`, `FOR UPDATE SKIP LOCKED`, advisory locks, arrays or temporary tables, and explicit version columns. Similar syntax is not sufficient; concurrency and failure behavior must satisfy the same public contract.

## Schema migration rules

- Migration execution is provider-specific and must be explicit.
- Application runtime identities must not receive implicit DDL permissions.
- Applied-migration journals do not replace drift or integrity checks.
- Ordered scripts, transaction boundaries, repeatability rules, and recovery steps must be documented by the provider.
- Cross-provider numbering may align for operator clarity, but scripts and SQL remain provider-owned.

## Portability review

Before moving behavior into the provider-neutral layer, verify that it can be described without a vendor type, SQL construct, driver API, or provider-specific failure mode. Before adding a provider, map every claimed contract to native concurrency semantics and focused verification evidence. Unsupported capabilities must fail explicitly rather than silently approximating another provider.
