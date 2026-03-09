# Incursa.Platform.Dns

`Incursa.Platform.Dns` provides the layer 2, provider-neutral DNS capability for Incursa Platform.

## What It Owns

- the local source of truth for DNS zones and DNS records
- provider-neutral models for `A`, `AAAA`, `CNAME`, `TXT`, and `MX` records
- storage-backed zone, record, and query services
- normalization, delete, and reconcile semantics over the local model

## What It Does Not Own

- provider SDK types or provider-specific transport
- certificate onboarding or domain ownership workflows
- vendor-specific orchestration beyond the generalized DNS model

## Registration

```csharp
services.AddDns();
```

`AddDns()` registers the public `IDnsZoneService`, `IDnsRecordService`, and `IDnsQueryService` abstractions. The host is expected to provide the required `Incursa.Platform.Storage` record and lookup stores.
