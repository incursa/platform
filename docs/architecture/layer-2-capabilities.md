# Layer 2 Capability Families

`Incursa.Platform` distinguishes between:

- layer 2 capability packages in `src/` that define the public, provider-neutral domain and service surface
- layer 1 provider materializations that hang under those capabilities as focused adapters
- vendor-specific public integration packages that may sit beside the layer 2 families when they are not provider-neutral capabilities

When a package is a public layer 1 vendor adapter rather than a provider-neutral capability, prefer the `Incursa.Integrations.*` naming family. Vendor-specific does not imply incubating.

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

### `src/Incursa.Integrations.WorkOS.Access/`

Owns:

- mapping WorkOS organization memberships into the local access model
- provider alias resolution from the authoritative local registry
- deterministic provider-managed membership and assignment identifiers
- optional scope-root materialization and reconciliation work-item hooks

Does not own:

- canonical access storage outside `Incursa.Platform.Access`
- claims middleware, widget integration, webhook pipelines, or broader WorkOS app-auth concerns

### `src/Incursa.Platform.Access.AspNetCore/`

Owns:

- request-time access context resolution for ASP.NET Core
- claims, route, and query mapping into the canonical `Incursa.Platform.Access` model
- current access subject, scope-root, and tenant resolution backed by `IAccessQueryService`
- personal-scope fallback and WorkOS-friendly organization-claim defaults without duplicating the access model

Does not own:

- a second identity or permission model
- cookie/session onboarding workflows or app-auth redirect behavior
- WorkOS management/profile hydration or broader middleware sprawl

### Related public WorkOS packages

Outside the focused access adapter boundary, WorkOS also ships public layer 1 packages for:

- app-auth and ASP.NET Core middleware
- cookie/session selection, onboarding enforcement, and broader app-auth behavior
- widgets, management clients, profile/session enrichment, and other workflow-heavy vendor surfaces

## Webhook capability family

### `src/Incursa.Platform.Webhooks/`

Owns:

- provider-neutral webhook envelopes, ingestion, and processing contracts
- inbox-backed dedupe and retry handling
- provider registry and handler composition

Does not own:

- provider-specific signature algorithms or payload parsing rules
- vendor-owned endpoint pipelines or dedupe stores

### `src/Incursa.Integrations.WorkOS.Webhooks/`

Owns:

- WorkOS signature validation over the shared webhook authenticator contract
- WorkOS payload classification into provider event id, event type, dedupe key, and partition key
- provider registration that plugs into the existing `Incursa.Platform.Webhooks` pipeline

Does not own:

- a separate ASP.NET Core endpoint, processor, or persistence model
- broader WorkOS management, widget, or app-auth behavior
- transactional guarantees beyond the shared inbox/idempotency pipeline

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

### `src/Incursa.Integrations.Cloudflare.Dns/`

Owns:

- DNS-only Cloudflare HTTP translation for zones and records
- provider-to-capability mapping for record list, upsert, delete, and reconcile operations
- provider-managed external-link materialization without exposing Cloudflare-specific types in the public DNS surface

Does not own:

- custom hostname onboarding
- KV, R2, or storage adapters
- load-balancing, probe, or non-DNS Cloudflare capabilities

### Related public Cloudflare packages

Outside the focused DNS adapter boundary, Cloudflare also ships public layer 1 packages for:

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

### `src/Incursa.Integrations.Cloudflare.CustomDomains/`

Owns:

- the Cloudflare custom-hostname adapter for the public custom-domain capability
- translation between Cloudflare payloads and the local `CustomDomain` model
- provider synchronization hooks that upsert Cloudflare state into the local source of truth

Does not own:

- KV or R2 storage adapters
- load-balancing, probes, or broader Cloudflare umbrella registration
- a second parallel custom-domain model

### Related public Cloudflare packages

Outside the focused DNS/custom-domain boundaries, Cloudflare also ships public layer 1 packages for:

- KV, R2, and storage-oriented adapters
- load-balancing and probe utilities
- broader Cloudflare umbrella registration and any workflow-heavy vendor code that is not cleanly DNS or custom-domain scoped

## Remaining layer 2 deferrals

The remaining WorkOS material is intentionally not being promoted into a standalone identity layer 2 package. The promoted slices are intentionally small:

- `Incursa.Platform.Access.AspNetCore` for request-time access context integration
- `Incursa.Integrations.WorkOS.Webhooks` for thin webhook authentication/classification

The following stay as public vendor-specific WorkOS packages rather than becoming a new layer 2 capability:

- cookie/session organization selection and onboarding middleware
- widgets, profile enrichment, and broader app-auth workflows
- management clients and other vendor-heavy operational surfaces
- the old vendor-owned webhook endpoint/processor/dedupe pipeline

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
services.AddAccessAspNetCore(options =>
{
    options.ScopeRootExternalLinkProvider = "workos";
    options.ScopeRootExternalLinkResourceType = "organization";
});
```

Webhooks:

```csharp
services.AddIncursaWebhooks();

services.AddWorkOsWebhooks(options =>
{
    options.SigningSecret = configuration["WorkOS:WebhookSigningSecret"]!;
});
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
