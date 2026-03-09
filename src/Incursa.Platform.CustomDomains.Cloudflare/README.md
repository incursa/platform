# Incursa.Platform.CustomDomains.Cloudflare

`Incursa.Platform.CustomDomains.Cloudflare` maps the provider-neutral `Incursa.Platform.CustomDomains` model onto Cloudflare custom-hostname APIs.

## What It Owns

- Cloudflare HTTP integration for custom hostnames
- translation between Cloudflare payloads and `CustomDomain`
- synchronization of provider state into the local custom-domain model

## What It Leaves In Incubating

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
