# Incursa.Integrations.Cloudflare.CustomDomains

`Incursa.Integrations.Cloudflare.CustomDomains` maps the provider-neutral `Incursa.Platform.CustomDomains` model onto Cloudflare custom-hostname APIs.

## Where It Fits

Use this package when your application wants the provider-neutral custom-domain model from `Incursa.Platform.CustomDomains`, but Cloudflare is the external provider responsible for hostname creation, verification, and provider-state synchronization.

## What It Owns

- Cloudflare HTTP integration for custom hostnames
- translation between Cloudflare payloads and `CustomDomain`
- synchronization of provider state into the local custom-domain model

## What It Leaves To Other Cloudflare Packages

- KV and R2 storage adapters
- load-balancing and probe clients
- broader Cloudflare umbrella registration

## Registration

```csharp
services.AddCustomDomains();

services.AddCloudflareCustomDomains(options =>
{
    options.ApiToken = configuration["Cloudflare:ApiToken"]!;
    options.ZoneId = configuration["Cloudflare:ZoneId"]!;
});
```
