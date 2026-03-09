# Incursa.Platform.Dns

`Incursa.Platform.Dns` is the layer 2, provider-neutral DNS capability for the monorepo. It models zones and records in a provider-neutral way so that local application state stays stable even when the backing DNS provider changes.

## When To Start Here

Start here when you need a reusable DNS model, zone administration, record management, or reconciliation behavior that should not leak Cloudflare or any other provider directly into application code.

## What It Owns

- the local source of truth for DNS zones and DNS records
- provider-neutral models for `A`, `AAAA`, `CNAME`, `TXT`, and `MX` records
- storage-backed zone, record, and query services
- normalization, delete, and reconcile semantics over the local model

## What It Does Not Own

- provider SDK types or provider-specific transport
- certificate onboarding or domain ownership workflows
- vendor-specific orchestration beyond the generalized DNS model

## Related Packages

- `Incursa.Integrations.Cloudflare.Dns` for Cloudflare DNS integration
- `Incursa.Platform.CustomDomains` for custom-hostname lifecycle state built above or beside DNS
- `Incursa.Platform.Storage` for the backing record and lookup stores

## Registration

```csharp
services.AddDns();
```

`AddDns()` registers the public `IDnsZoneService`, `IDnsRecordService`, and `IDnsQueryService` abstractions. The host is expected to provide the required `Incursa.Platform.Storage` record and lookup stores.
