# Incursa.Platform.Access.AspNetCore

`Incursa.Platform.Access.AspNetCore` resolves the current authenticated access context for ASP.NET Core requests by reusing the canonical `Incursa.Platform.Access` model.

## Install

```bash
dotnet add package Incursa.Platform.Access.AspNetCore
```

## What It Owns

- current request access context resolution
- claim, route, and query mapping into the existing access capability
- personal-scope and organization-scope resolution against `IAccessQueryService`

It does not introduce a second identity or permission model. The canonical source of truth remains `Incursa.Platform.Access`.

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
