# Incursa.Platform.Dns.Cloudflare

`Incursa.Platform.Dns.Cloudflare` maps the provider-neutral `Incursa.Platform.Dns` model onto Cloudflare DNS HTTP APIs.

## What It Owns

- Cloudflare HTTP integration for DNS zones and DNS records
- translation between Cloudflare DNS payloads and `DnsZone` / `DnsRecord`
- reconcile, upsert, query, and delete behavior over Cloudflare DNS

## What It Leaves In Incubating

- custom-hostname onboarding and ownership workflows
- certificate orchestration
- load-balancing, KV, R2, and non-DNS Cloudflare features

## Registration

```csharp
services.AddCloudflareDns(options =>
{
    options.ApiToken = configuration["Cloudflare:ApiToken"]!;
    options.ZoneId = configuration["Cloudflare:ZoneId"];
});
```

The adapter only exposes `Incursa.Platform.Dns` types. Cloudflare SDK types do not leak into the capability surface.
