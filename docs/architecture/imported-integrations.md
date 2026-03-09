# Imported Integrations

This repository preserves the provenance and landing zone of the sibling integration repositories that were folded into the monorepo.

## `C:\src\incursa\integrations-postmark`

Decision:

- merge the provider-agnostic email capability and the Postmark adapter into the public monorepo

Resulting packages:

- `src/Incursa.Platform.Email/`
- `src/Incursa.Platform.Email.AspNetCore/`
- `src/Incursa.Platform.Email.Postmark/`
- `src/Incursa.Platform.Email.SqlServer/`
- `src/Incursa.Platform.Email.Postgres/`
- `tests/Incursa.Platform.Email.Tests/`

Notes:

- `Incursa.Email.Abstractions` and `Incursa.Email.Core` were consolidated into `Incursa.Platform.Email`
- package and namespace identity was normalized to the `Incursa.Platform.Email*` family
- the imported email test slice was rewritten to the repo's xUnit-based test conventions

## `C:\src\incursa\integrations-workos`

Decision:

- keep `Incursa.Platform.Access` as the provider-neutral source-of-truth capability
- promote the remaining WorkOS vendor packages into public `Incursa.Integrations.WorkOS.*` packages

Result:

- capability packages:
  - `src/Incursa.Platform.Access/`
  - `src/Incursa.Platform.Access.AspNetCore/`
- public vendor packages:
  - `src/Incursa.Integrations.WorkOS/`
  - `src/Incursa.Integrations.WorkOS.Abstractions/`
  - `src/Incursa.Integrations.WorkOS.Access/`
  - `src/Incursa.Integrations.WorkOS.AspNetCore/`
  - `src/Incursa.Integrations.WorkOS.Audit/`
  - `src/Incursa.Integrations.WorkOS.Webhooks/`
- tests:
  - `tests/Incursa.Platform.Access.Tests/`
  - `tests/Incursa.Platform.Access.AspNetCore.Tests/`
  - `tests/Incursa.Integrations.WorkOS.Tests/`
  - `tests/Incursa.Integrations.WorkOS.Webhooks.Tests/`

Notes:

- public layer 1 WorkOS packages use the `Incursa.Integrations.WorkOS.*` family
- the associated provider-neutral capability and hosting packages remain in `Incursa.Platform.*`

## `C:\src\incursa\integrations-cloudflare`

Decision:

- keep `Incursa.Platform.Dns` and `Incursa.Platform.CustomDomains` as provider-neutral capabilities
- promote the remaining Cloudflare vendor packages into public `Incursa.Integrations.Cloudflare.*` packages

Result:

- capability packages:
  - `src/Incursa.Platform.CustomDomains/`
  - `src/Incursa.Platform.Dns/`
- public vendor packages:
  - `src/Incursa.Integrations.Cloudflare/`
  - `src/Incursa.Integrations.Cloudflare.CustomDomains/`
  - `src/Incursa.Integrations.Cloudflare.Dns/`
  - `src/Incursa.Integrations.Cloudflare.KvProbe/`
- tests:
  - `tests/Incursa.Platform.CustomDomains.Tests/`
  - `tests/Incursa.Platform.Dns.Tests/`
  - `tests/Incursa.Integrations.Cloudflare.Tests/`
  - `tests/Incursa.Integrations.Cloudflare.IntegrationTests/`

Notes:

- Cloudflare-specific adapters and broader vendor primitives now live in `Incursa.Integrations.Cloudflare.*`
- `Incursa.Integrations.Cloudflare.KvProbe` remains a public source-tree executable and is intentionally non-packable

## `C:\src\incursa\integrations-electronicnotary`

Decision:

- promote the vendor packages into public `Incursa.Integrations.ElectronicNotary.*` packages

Result:

- public vendor packages:
  - `src/Incursa.Integrations.ElectronicNotary/`
  - `src/Incursa.Integrations.ElectronicNotary.Abstractions/`
  - `src/Incursa.Integrations.ElectronicNotary.Proof/`
  - `src/Incursa.Integrations.ElectronicNotary.Proof.AspNetCore/`
- tests:
  - `tests/Incursa.Integrations.ElectronicNotary.Tests/`

Notes:

- these remain vendor-specific layer 1 integrations
- they are public and packable even where the provider surface is specialized

## Summary

- no imported repository was deleted
- provider-neutral capability families remain in `Incursa.Platform.*`
- vendor-specific public packages now live in `src/` under `Incursa.Integrations.*`
- `incubating/` is reserved for future staging code rather than active vendor package families
