# Incursa.Platform.CustomDomains

`Incursa.Platform.CustomDomains` provides the layer 2, provider-neutral capability for managed custom-domain and custom-hostname lifecycle state.

## When To Start Here

Start here when your application needs to manage customer-owned domains or hostnames but you do not want the public model tied directly to a single provider’s API or terminology.

## What It Owns

- the local source of truth for managed custom domains
- provider-neutral lifecycle, certificate, and ownership-verification models
- storage-backed administration and query services
- hostname normalization and external-link lookup behavior

## What It Does Not Own

- provider SDK types or transport
- DNS record management
- certificate issuance workflows beyond provider-neutral status

## Related Packages

- `Incursa.Integrations.Cloudflare.CustomDomains` for Cloudflare custom-hostname integration
- `Incursa.Platform.Dns` for zone and record management
- `Incursa.Platform.Access` if domain ownership is tied to scope roots or tenants

## Registration

```csharp
services.AddCustomDomains();
```

`AddCustomDomains()` registers the public `ICustomDomainAdministrationService` and `ICustomDomainQueryService` abstractions. The host is expected to provide the required `Incursa.Platform.Storage` record and lookup stores.
