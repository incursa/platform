# Incursa.Platform.Storage

`Incursa.Platform.Storage` provides the shared storage substrate used by the platform capability packages and many vendor adapters. It is intentionally opinionated about partitions, consistency, work queues, and coordination so higher-level packages can build on stable primitives instead of inventing their own store abstractions.

## When To Start Here

Start here when you are building either:

- a layer 2 capability that needs provider-neutral record, lookup, payload, work, or coordination storage
- a layer 1 integration package that needs to plug a concrete storage provider into the shared platform substrate

## What Belongs Here

- small consumer-facing abstractions for storage keys, optimistic concurrency, and consistency intent
- provider-neutral contracts for record stores, lookup stores, payload stores, work stores, and coordination stores
- shared result types and storage-specific exceptions

## What Does Not Belong Here

- Azure, SQL, or other provider SDK types
- ad hoc querying, `IQueryable`, or ORM-style mapping layers
- domain-specific account, tenant, billing, or integration-specific models

## Guarantees

- optimistic concurrency is expressed through opaque provider-managed ETags
- partition-bounded scans are explicit and intentionally narrow
- same-partition atomic intent is modeled separately from cross-partition eventual consistency intent
- work queue semantics are claim, complete, and abandon rather than strict FIFO processing

## Related Packages

- `Incursa.Integrations.Storage.Azure` for the Azure-backed implementation
- `Incursa.Platform.Access`, `Incursa.Platform.Dns`, and `Incursa.Platform.CustomDomains` as examples of capabilities built on this substrate
