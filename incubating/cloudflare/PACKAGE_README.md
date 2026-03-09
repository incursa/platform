# Incursa.Integrations.Cloudflare

Shared Cloudflare integration package used across Incursa services.

## Included capabilities

- Cloudflare R2 storage helpers
- Cloudflare KV typed client and store helpers
- SSL for SaaS custom hostname onboarding/sync services
- Cloudflare load balancer monitor/pool/load-balancer typed clients
- ASP.NET Core DI extensions for centralized configuration

## Required configuration

Use the `Cloudflare` configuration section with API token auth and feature-specific subsections (`R2`, `KV`, `CustomHostnames`, `LoadBalancing`).
`Cloudflare:ForceIpv4` defaults to `true`; set it to `false` to allow standard dual-stack connection behavior.
`Cloudflare:RequestTimeoutSeconds` controls the maximum duration for each Cloudflare API call, including response body reads.
For KV listings, `Cloudflare:KV:ListOperationTimeoutSeconds` is an additional operation-level cap for key-list scans.

See the repository `README.md` for full configuration examples.

## Support

This package is intended for internal Incursa usage and is published to GitHub Packages and the private Incursa feed.

## Unit testing with in-memory storage

Use `AddCloudflareInMemoryStorage()` after `AddCloudflareIntegration(...)` to replace cloud-backed storage implementations with deterministic in-memory versions:

- `ICloudflareKvStore` -> `InMemoryCloudflareKvStore`
- `ICloudflareR2BlobStore` -> `InMemoryCloudflareR2BlobStore`
