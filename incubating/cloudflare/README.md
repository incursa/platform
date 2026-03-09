# Incursa Integrations Cloudflare

Shared Cloudflare integration library for Incursa applications.

## Capabilities

- Cloudflare R2 object storage helpers (`ICloudflareR2BlobStore`)
- Cloudflare KV helpers and typed client (`ICloudflareKvStore`, `ICloudflareKvClient`)
- SSL for SaaS custom hostname client + onboarding/sync services
- Cloudflare Load Balancer clients:
  - monitors (`ICloudflareLoadBalancerMonitorClient`)
  - pools (`ICloudflareLoadBalancerPoolClient`)
  - load balancers (`ICloudflareLoadBalancerClient`)

## Installation

```xml
<PackageReference Include="Incursa.Integrations.Cloudflare" Version="<version>" />
```

## Configuration

```json
{
  "Cloudflare": {
    "BaseUrl": "https://api.cloudflare.com/client/v4",
    "ApiToken": "<token>",
    "AccountId": "<account-id>",
    "ZoneId": "<zone-id>",
    "ForceIpv4": true,
    "RequestTimeoutSeconds": 8,
    "RetryCount": 2,
    "KV": {
      "AccountId": "<account-id>",
      "NamespaceId": "<namespace-id>",
      "ListOperationTimeoutSeconds": 5
    },
    "R2": {
      "Endpoint": "https://<account>.r2.cloudflarestorage.com",
      "AccessKeyId": "<access-key>",
      "SecretAccessKey": "<secret>",
      "Bucket": "<bucket>",
      "Region": "auto"
    },
    "CustomHostnames": {
      "ZoneId": "<zone-id>"
    },
    "LoadBalancing": {
      "AccountId": "<account-id>",
      "ZoneId": "<zone-id>"
    }
  }
}
```

## DI setup

```csharp
using Incursa.Integrations.Cloudflare.DependencyInjection;

builder.Services.AddCloudflareIntegration(builder.Configuration);
```

You can also configure individual sections using:

- `AddCloudflareKv(...)`
- `AddCloudflareR2(...)`
- `AddCloudflareCustomHostnames(...)`
- `AddCloudflareLoadBalancing(...)`

## Example: Custom hostname onboarding

```csharp
var onboarding = serviceProvider.GetRequiredService<ICloudflareDomainOnboardingService>();
var result = await onboarding.CreateOrFetchCustomHostnameAsync("tenant.example.com", cancellationToken);

// result.OwnershipVerificationName / result.OwnershipVerificationValue -> DNS record guidance
```

## Example: KV

```csharp
var kv = serviceProvider.GetRequiredService<ICloudflareKvStore>();
await kv.PutAsync("org:123", "active", cancellationToken);
var value = await kv.GetAsync("org:123", cancellationToken);
```

## Testing with in-memory storage

For unit tests, you can replace Cloudflare-backed storage with deterministic in-memory implementations:

```csharp
using Incursa.Integrations.Cloudflare.DependencyInjection;

var services = new ServiceCollection();
services.AddLogging();
services.AddCloudflareIntegration(configuration);
services.AddCloudflareInMemoryStorage();
```

This swaps:

- `ICloudflareKvStore` -> `InMemoryCloudflareKvStore`
- `ICloudflareR2BlobStore` -> `InMemoryCloudflareR2BlobStore`

## Manual Real-KV Probe

Use the built-in probe to test Cloudflare KV against real credentials from a local file:

```powershell
pwsh -File ./scripts/run-kv-probe.ps1 -ConfigPath "C:\path\to\kv-probe.json"
```

Reference:

- `docs/50-runbooks/cloudflare-kv-probe.md`
- `docs/50-runbooks/cloudflare-kv-probe.sample.json`

## Live Integration Test Secrets

Nightly/manual deep quality workflows run Cloudflare live integration tests using a protected GitHub environment named `cloudflare-integration-tests`.

Expected environment secrets:

- `CF_TEST_API_TOKEN`
- `CF_TEST_ACCOUNT_ID`
- `CF_TEST_ZONE_ID`
- `CF_TEST_KV_NAMESPACE_ID`

Optional environment variable:

- `CF_TEST_BASE_URL` (defaults to `https://api.cloudflare.com/client/v4`)

## Example: Load balancer

```csharp
var lbClient = serviceProvider.GetRequiredService<ICloudflareLoadBalancerClient>();
var all = await lbClient.ListAsync(cancellationToken);
```

## Notes

- Auth model is Cloudflare API token only (Bearer token).
- `Cloudflare:ForceIpv4` defaults to `true`; set it to `false` to allow standard dual-stack (IPv4/IPv6) connection behavior.
- The package uses a shared retry policy for transient Cloudflare/API network failures.
- `Cloudflare:RequestTimeoutSeconds` bounds the full API operation (request + response body read) and defaults to `8`.
- `Cloudflare:KV:ListOperationTimeoutSeconds` is an additional cap for key-list scans; use it to constrain long cursor walks.
- For custom domains, this package surfaces ownership verification records and SSL status from Cloudflare.
