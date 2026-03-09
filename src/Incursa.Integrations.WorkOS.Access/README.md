# Incursa.Integrations.WorkOS.Access

`Incursa.Integrations.WorkOS.Access` adapts WorkOS organization memberships onto the provider-neutral `Incursa.Platform.Access` model.

It is the WorkOS-facing access adapter for the monorepo. The package exists so WorkOS concepts can be reconciled into the local access source of truth without turning WorkOS into the primary access model.

## What It Owns

- translation from WorkOS organization memberships into local scope memberships and role assignments
- WorkOS-specific alias defaults for organization external links and role lookup
- deterministic provider-managed identifiers for membership and assignment reconciliation
- optional reconciliation work-item hooks when provider snapshots should be revisited

## What It Does Not Own

- the canonical permission or role registry
- auth middleware, claims enrichment, or WorkOS session handling
- direct source-of-truth access storage outside `Incursa.Platform.Access`

## When To Use It

Use this package when your application keeps its canonical authorization state in `Incursa.Platform.Access` but needs to synchronize or interpret WorkOS organization membership data.

## Usage

Register the core access capability first, then register the WorkOS adapter:

```csharp
services.AddAccess(registry =>
{
    registry.AddPermission(new AccessPermissionDefinition(
        new AccessPermissionId("tenant.read"),
        "Read tenant"));

    registry.AddRole(new AccessRoleDefinition(
        new AccessRoleId("organization-admin"),
        "Organization administrator",
        [new AccessPermissionId("tenant.read")],
        providerAliases: new Dictionary<string, string>
        {
            [WorkOsAccessDefaults.RoleAliasKey] = "org_admin",
        }));
});

services.AddWorkOsAccess();
```

WorkOS roles are resolved through provider aliases on the local registry definitions; the adapter does not maintain a second role registry.

Existing scope roots can carry a WorkOS organization link up front:

```csharp
var scopeRoot = new ScopeRoot(
    new ScopeRootId("org-123"),
    ScopeRootKind.Organization,
    "Contoso",
    externalLinks:
    [
        new ExternalLink(
            new ExternalLinkId("workos-org-123"),
            WorkOsAccessDefaults.ProviderName,
            "org_01H...",
            WorkOsAccessDefaults.OrganizationResourceType),
    ]);
```

If a matching scope root does not exist, the adapter can materialize an organization scope root from the WorkOS organization id during synchronization.

## Consistency Model

- local access records remain the source of truth
- WorkOS-managed memberships and assignments are reconciled by deterministic local identifiers
- scope-root creation, membership updates, and projection updates still follow the core package's eventual-consistency model
- broader WorkOS claim/session middleware and application-specific organization-to-tenant collapse patterns now live in the public `Incursa.Integrations.WorkOS*` packages

## Related Packages

- `Incursa.Platform.Access`
- `Incursa.Platform.Access.AspNetCore`
- `Incursa.Integrations.WorkOS`
- `Incursa.Integrations.WorkOS.AspNetCore`
