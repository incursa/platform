# Outbox specification index

This directory contains the Outbox component specification and related provider and coordination contracts.

## Authoritative specifications

- [`outbox-specification.md`](outbox-specification.md): functional specification for Outbox behavior, API surface, parameter semantics, scheduling, persistence, and behavioral requirements.
- [`join-coordination-specification.md`](join-coordination-specification.md): independent Join coordination contract.
- [`providers/sqlserver-primitives-spec.md`](providers/sqlserver-primitives-spec.md): SQL Server primitive contracts.
- [`providers/postgres-primitives-spec.md`](providers/postgres-primitives-spec.md): PostgreSQL primitive contracts.
- [`providers/inmemory-primitives-spec.md`](providers/inmemory-primitives-spec.md): in-memory primitive contracts.
- [`providers/provider-conformance-matrix.md`](providers/provider-conformance-matrix.md): scenario-to-test traceability across providers.

The specification files are authoritative for supported behavior. Point-in-time implementation status, open work, and validation outcomes belong in work items, verification records, or pull requests.

## Outbox navigation

- [Architecture and scope](outbox-specification.md#2-purpose-and-scope)
- [Key concepts](outbox-specification.md#4-key-concepts-and-terms)
- [API surface](outbox-specification.md#5-public-api-surface)
- [Behavioral requirements](outbox-specification.md#6-behavioral-requirements)
- [Database schema reference](outbox-specification.md#appendix-a-database-schema-reference)
- [Handler example](outbox-specification.md#appendix-b-handler-implementation-example)
- [Multi-tenant example](outbox-specification.md#appendix-c-multi-tenant-usage-example)

## Historical design record

[`SPEC_UPDATE_SUMMARY.md`](SPEC_UPDATE_SUMMARY.md) records the completed separation of Join concerns and parameter-semantics normalization. Treat it as change history, not as the source of current implementation or validation status.
