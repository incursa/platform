# Incursa.Integrations.Storage.Azure

`Incursa.Integrations.Storage.Azure` is the Azure-backed layer 1 storage adapter for `Incursa.Platform.Storage`. It supplies the concrete Azure Tables, Blobs, and Queues implementation behind the shared storage substrate.

## Where It Fits

Use this package when the public capability or application code is already written against `Incursa.Platform.Storage` and Azure is the concrete backing store you want to plug in.

## What Belongs Here

- the Azure-backed implementation of the shared storage substrate
- Azure-specific options, DI registration, serialization, and internal mapping logic
- internal support for Azure Table entities, blob metadata, queue envelopes, and blob-lease coordination

## What Does Not Belong Here

- public provider-agnostic contracts
- Azure SDK types on the shared storage contract surface
- domain-specific tenant, billing, or workflow abstractions

## Registration

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
