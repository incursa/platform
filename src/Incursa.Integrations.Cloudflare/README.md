# Incursa.Integrations.Cloudflare

`Incursa.Integrations.Cloudflare` is the top-level family package for Cloudflare-specific integrations in this monorepo.

## How This Family Fits

The Cloudflare packages are vendor-specific layer 1 adapters. They complement the provider-neutral capability packages without redefining them:

- `Incursa.Platform.Dns` owns the provider-neutral DNS model
- `Incursa.Platform.CustomDomains` owns the provider-neutral custom-domain model

Cloudflare-specific APIs, payload translation, and transport concerns belong in `Incursa.Integrations.Cloudflare.*`.

## Packages In This Family

- `Incursa.Integrations.Cloudflare.Dns` for Cloudflare DNS operations over the `Incursa.Platform.Dns` model
- `Incursa.Integrations.Cloudflare.CustomDomains` for Cloudflare custom-hostname flows over the `Incursa.Platform.CustomDomains` model
- `Incursa.Integrations.Cloudflare.KvProbe` for Cloudflare KV-related probing and vendor-specific support code

## What This Family Does Not Try To Do

- define a second DNS model
- define a second custom-domain model
- act as a general-purpose Cloudflare umbrella abstraction
