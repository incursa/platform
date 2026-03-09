# Incursa.Platform.Access.AspNetCore

`Incursa.Platform.Access.AspNetCore` is the ASP.NET Core hosting adapter for the `Incursa.Platform.Access` capability. It resolves the current request’s access context without introducing a second identity or permission model.

## What It Owns

- current request access context resolution
- claim, route, and query mapping into the existing access capability
- personal-scope and organization-scope resolution against `IAccessQueryService`

## What It Does Not Own

- the canonical access registry or access state model
- generic ASP.NET authorization policy plumbing
- provider-specific session or management APIs

## Related Packages

- `Incursa.Platform.Access` for the source-of-truth access model
- `Incursa.Integrations.WorkOS.AspNetCore` when request context originates from WorkOS session or claim material
- `Incursa.Integrations.WorkOS.Access` when WorkOS organizations and roles need to synchronize into the local access model

## Typical Use

```csharp
services.AddAccess(registry =>
{
    registry.AddPermission("tenant.read", "Read tenant");
    registry.AddRole("organization-member", "Organization member", "tenant.read");
});

services.AddAccessAspNetCore(options =>
{
    options.ScopeRootExternalLinkProvider = "workos";
    options.ScopeRootExternalLinkResourceType = "organization";
});
```

Resolve the current request context from DI:

```csharp
var accessContext = await accessor.GetCurrentAsync(cancellationToken);
```
