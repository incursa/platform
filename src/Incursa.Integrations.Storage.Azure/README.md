# Incursa.Integrations.Storage.Azure

Azure Tables, Blobs, and Queues provider for `Incursa.Platform.Storage`.

## Install

```bash
dotnet add package Incursa.Integrations.Storage.Azure
```

## What belongs here

- The Azure-backed implementation of the shared storage substrate.
- Azure-specific options, DI registration, serialization, and internal mapping logic.
- Internal support for Azure Table entities, blob metadata, queue envelopes, and blob-lease coordination.

## What does not belong here

- Public provider-agnostic contracts. Those belong in `Incursa.Platform.Storage`.
- Azure SDK types on the shared storage contract surface.
- Domain-specific account, tenant, billing, or workflow abstractions.

## Intended usage

Register the provider once, then resolve `IRecordStore<T>`, `ILookupStore<T>`, `IPayloadStore`, `IWorkStore<T>`, and `ICoordinationStore` from DI.

```csharp
builder.Services.AddAzureStorage(
    new AzureStorageOptions
    {
        ConnectionString = builder.Configuration["Storage:ConnectionString"],
        CreateResourcesIfMissing = true,
        PayloadContainerName = "payloads",
        CoordinationTableName = "coordination",
    });
```

### Record store usage

```csharp
StorageRecordKey key = new(new StoragePartitionKey("tenant-a"), new StorageRowKey("order-42"));

StorageItem<OrderRecord> saved = await recordStore.WriteAsync(
    key,
    order,
    StorageWriteMode.Put,
    StorageWriteCondition.IfNotExists(),
    cancellationToken);

await foreach (StorageItem<OrderRecord> item in recordStore.QueryPartitionAsync(
    new StoragePartitionKey("tenant-a"),
    StoragePartitionQuery.WithPrefix("order-"),
    cancellationToken))
{
    // consume item
}
```

### Payload store usage

```csharp
StoragePayloadKey payloadKey = new("orders", "order-42.json");

await payloadStore.WriteJsonAsync(
    payloadKey,
    orderPayload,
    new PayloadWriteOptions { SchemaVersion = "v1" },
    StorageWriteCondition.Unconditional(),
    cancellationToken);

PayloadMetadata? metadata = await payloadStore.GetMetadataAsync(payloadKey, cancellationToken);
```

### Work store usage

```csharp
await workStore.EnqueueAsync(
    new WorkItem<OrderWork>("job-42", work, correlationId: "corr-42"),
    new WorkEnqueueOptions { InitialVisibilityDelay = TimeSpan.FromSeconds(5) },
    cancellationToken);

ClaimedWorkItem<OrderWork>? claimed = await workStore.ClaimAsync(
    new WorkClaimOptions(TimeSpan.FromSeconds(30)),
    cancellationToken);
```

### Coordination store usage

```csharp
bool created = await coordinationStore.TryCreateIdempotencyMarkerAsync(key, cancellationToken);

CoordinationLease? lease = await coordinationStore.TryAcquireLeaseAsync(
    new CoordinationLeaseRequest(key, "worker-a", TimeSpan.FromSeconds(30)),
    cancellationToken);
```

## Guarantees

- Records and lookups are stored in Azure Tables with exact-key access and partition-bounded scans only.
- Payloads are stored in Azure Blobs with metadata reads that do not require reading the payload body.
- Work items are stored in Azure Queues with claim, complete, and abandon semantics rather than strict FIFO guarantees.
- Coordination uses Azure-native constructs: Table entities for markers/checkpoints and blob leases for advisory mutual exclusion.
- Same-partition record batches can be atomic; cross-partition consistency is eventual and unsupported as a fake transaction.

## Non-goals

- No LINQ provider or expression translation layer.
- No cross-partition distributed transaction abstraction.
- No domain-specific Stripe, WorkOS, billing, or tenant conventions.
