# Incursa.Platform.Access

`Incursa.Platform.Access` is the layer 2 access and authorization capability for the monorepo. It provides the provider-neutral source of truth for users, scope roots, tenants, memberships, assignments, explicit grants, and effective-access evaluation.

## When To Start Here

Start here when you need to model application access state in a way that is independent of any one provider. This is the package that owns the access domain model. Vendor adapters such as WorkOS should map into this model rather than replace it.

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

## Related Packages

- `Incursa.Platform.Access.AspNetCore` for request-time access-context resolution in ASP.NET Core
- `Incursa.Integrations.WorkOS.Access` for mapping WorkOS concepts into the local access model
- `Incursa.Platform.Audit` when access changes need immutable audit records

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
- provider synchronization should hang off explicit work and reconciliation flows rather than bypassing the local model
