# Layer 2 Capability Families

`Incursa.Platform` distinguishes between:

- layer 2 capability packages in `src/` that define the public, provider-neutral domain and service surface
- layer 1 provider materializations that hang under those capabilities as focused adapters
- incubating vendor buckets that remain preserved until their public boundary is clean

This document records the current layer 2 extraction for access/authorization, DNS, and custom domains.

## Access capability family

### `src/Incursa.Platform.Access/`

Owns:

- the authoritative local role and permission registry
- the local source-of-truth access model for users, scope roots, tenants, memberships, assignments, explicit grants, and audit entries
- deny-by-default effective access evaluation
- storage-backed administration and query services over `Incursa.Platform.Storage`
- append-only access audit journaling and projection maintenance

Does not own:

- WorkOS SDK or HTTP types
- auth middleware, claims/session enrichment, secrets, crypto, or password handling
- a second provider-specific role/permission registry
- SQL-specific storage assumptions

### `src/Incursa.Platform.Access.WorkOS/`

Owns:

- mapping WorkOS organization memberships into the local access model
- provider alias resolution from the authoritative local registry
- deterministic provider-managed membership and assignment identifiers
- optional scope-root materialization and reconciliation work-item hooks

Does not own:

- canonical access storage outside `Incursa.Platform.Access`
- claims middleware, widget integration, webhook pipelines, or broader WorkOS app-auth concerns

### What remains in `incubating/workos/`

Still incubating because it is broader than the clean access adapter boundary:

- app-auth and ASP.NET Core middleware
- claims/session and organization-context accessors
- widgets, webhook handling, management clients, and other workflow-heavy vendor surfaces

## DNS capability family

### `src/Incursa.Platform.Dns/`

Owns:

- the local source of truth for DNS zones and records
- provider-neutral models for `A`, `AAAA`, `CNAME`, `TXT`, and `MX` records
- storage-backed zone, record, query, delete, and reconcile operations
- normalization and projection behavior over `Incursa.Platform.Storage`

Does not own:

- Cloudflare SDK or transport details
- certificate onboarding, domain verification, or vendor-specific hostname orchestration
- non-DNS Cloudflare features

### `src/Incursa.Platform.Dns.Cloudflare/`

Owns:

- DNS-only Cloudflare HTTP translation for zones and records
- provider-to-capability mapping for record list, upsert, delete, and reconcile operations
- provider-managed external-link materialization without exposing Cloudflare-specific types in the public DNS surface

Does not own:

- custom hostname onboarding
- KV, R2, or storage adapters
- load-balancing, probe, or non-DNS Cloudflare capabilities

### What remains in `incubating/cloudflare/`

Still incubating because it is not yet a clean DNS adapter boundary:

- KV, R2, storage, load-balancing, and probe utilities
- broader vendor-bucket abstractions that should be split by capability before promotion

## Custom-domain capability family

### `src/Incursa.Platform.CustomDomains/`

Owns:

- the authoritative local model for managed custom domains and custom hostnames
- provider-neutral lifecycle, certificate, and ownership-verification state
- storage-backed administration and query services over `Incursa.Platform.Storage`
- hostname normalization and provider external-link materialization

Does not own:

- DNS zone/record administration
- provider HTTP or SDK types
- product-specific onboarding workflows or certificate issuance orchestration beyond provider-neutral state

### `src/Incursa.Platform.CustomDomains.Cloudflare/`

Owns:

- the Cloudflare custom-hostname adapter for the public custom-domain capability
- translation between Cloudflare payloads and the local `CustomDomain` model
- provider synchronization hooks that upsert Cloudflare state into the local source of truth

Does not own:

- KV or R2 storage adapters
- load-balancing, probes, or broader Cloudflare umbrella registration
- a second parallel custom-domain model

### What remains in `incubating/cloudflare/`

Still incubating because it is outside the clean public DNS/custom-domain boundary:

- KV, R2, and storage-oriented adapters
- load-balancing and probe utilities
- broader Cloudflare umbrella registration and any workflow-heavy vendor code that is not cleanly DNS or custom-domain scoped

## Deferred WorkOS candidates

The remaining WorkOS material is not being promoted into a new layer 2 package in this pass. The cleanest future candidates are:

- an access-focused ASP.NET Core/request-context adapter that hangs off `Incursa.Platform.Access`
- a provider-specific webhook adapter that hangs off `Incursa.Platform.Webhooks`

Those slices remain incubating until their public API is smaller and more clearly capability-oriented.

## Source of truth and synchronization

- Layer 2 capability packages own the canonical local model.
- Provider adapters synchronize into or out of that local model; they do not replace it.
- Provider links are carried as local external-link records so provider identifiers do not leak across the public surface.
- Provider synchronization is intentionally asynchronous/reconcilable rather than pretending cross-partition writes are transactional.

## Storage guarantees and non-goals

- Capability packages use `Incursa.Platform.Storage` abstractions for canonical records and projections.
- Projection updates are eventually consistent.
- Cross-partition writes are not transactional.
- Reconciliation hooks are explicit work items, not hidden side effects.
- Neither capability family assumes SQL Server, Postgres, or any other specific storage engine.

## Example usage

Access:

```csharp
services.AddAccess(registry =>
{
    registry.AddPermission("tenant.read", "Read tenant");
    registry.AddPermission("tenant.write", "Write tenant");
    registry.AddRole("tenant-admin", "Tenant administrator", "tenant.read", "tenant.write");
});

services.AddWorkOsAccess();
```

DNS:

```csharp
services.AddDns();

services.AddCloudflareDns(options =>
{
    options.ApiToken = configuration["Cloudflare:ApiToken"]!;
    options.ZoneId = configuration["Cloudflare:ZoneId"];
});
```

Custom domains:

```csharp
services.AddCustomDomains();

services.AddCloudflareCustomDomains(options =>
{
    options.ApiToken = configuration["Cloudflare:ApiToken"]!;
    options.ZoneId = configuration["Cloudflare:ZoneId"]!;
});
```
