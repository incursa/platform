# Incursa.Platform.Access

`Incursa.Platform.Access` provides a provider-neutral access capability for users, scope roots, tenants, memberships, assignments, explicit grants, and effective-access evaluation.

## What It Owns

- the authoritative local role and permission registry
- the local source-of-truth access model
- deny-by-default effective access evaluation
- storage-backed administration and query services
- an append-only access audit journal

## What It Does Not Own

- auth middleware or session handling
- crypto, secrets, or password flows
- provider-specific APIs

## Registration

Register a storage provider for `IRecordStore<T>`, `ILookupStore<T>`, `IWorkStore<T>`, and `ICoordinationStore`, then add the capability:

```csharp
services.AddAzureStorage(builder.Configuration);

services.AddAccess(registry =>
{
    registry.AddPermission("tenant.read", "Read tenant");
    registry.AddPermission("tenant.write", "Write tenant");
    registry.AddRole("tenant-admin", "Tenant administrator", "tenant.read", "tenant.write");
});
```

## Consistency Model

- canonical records are the source of truth
- lookup projections are updated with eventual consistency semantics
- cross-partition writes are not transactional
- provider synchronization should hang off explicit work/reconciliation flows rather than bypassing the local model
