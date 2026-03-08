# Incursa.Platform.Storage

Strongly opinionated, provider-agnostic storage contracts for partition-aware records, projections, payloads, work queues, and coordination.

## Install

```bash
dotnet add package Incursa.Platform.Storage
```

## What belongs here

- Small consumer-facing abstractions for storage keys, optimistic concurrency, and consistency intent.
- Provider-neutral contracts for record stores, lookup stores, payload stores, work stores, and coordination stores.
- Shared result types and storage-specific exceptions.

## What does not belong here

- Azure, SQL, or any other provider SDK types.
- Ad hoc querying, `IQueryable`, or ORM-style mapping layers.
- Domain-specific account, tenant, billing, or integration-specific models.

## Intended usage

Use this package from application code and from provider packages. Consumers depend on the interfaces here; provider packages supply the actual implementation and DI registration.

```csharp
StorageRecordKey customerKey = new(new StoragePartitionKey("customer"), new StorageRowKey("123"));
StorageWriteCondition createOnly = StorageWriteCondition.IfNotExists();

StorageItem<CustomerProjection>? projection = await lookupStore.GetAsync(customerKey, cancellationToken);
StorageItem<CustomerRecord> updated = await recordStore.WriteAsync(
    customerKey,
    customerRecord,
    StorageWriteMode.Put,
    createOnly,
    cancellationToken);
```

## Guarantees

- Optimistic concurrency is expressed through opaque provider-managed ETags.
- Partition-bounded scans are explicit and intentionally narrow: full partition, row-key prefix, or bounded row-key range.
- Same-partition atomic intent is modeled separately from cross-partition eventual consistency intent.
- Work queue semantics are claim, complete, and abandon; they do not promise strict FIFO processing.

## Non-goals

- No fake ORM or generic repository over arbitrary backends.
- No cross-partition transaction abstraction.
- No provider-specific SDK leakage on the public API surface.
