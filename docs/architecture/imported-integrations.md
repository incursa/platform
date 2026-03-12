# Imported Integration Provenance

This document preserves the provenance of the integration repositories that were previously folded into `platform` and are now split back out to `integrations-public`.

## `C:\src\incursa\integrations-postmark`

Provider-neutral packages retained in `platform`:

- `src/Incursa.Platform.Email/`
- `src/Incursa.Platform.Email.AspNetCore/`

Public provider implementations moved to `integrations-public`:

- `src/Incursa.Platform.Email.Postmark/`
- `src/Incursa.Platform.Email.SqlServer/`
- `src/Incursa.Platform.Email.Postgres/`
- `tests/Incursa.Platform.Email.Tests/`

## `C:\src\incursa\integrations-workos`

Provider-neutral packages retained in `platform`:

- `src/Incursa.Platform.Access/`
- `src/Incursa.Platform.Access.AspNetCore/`
- `src/Incursa.Platform.Access.Razor/`

Public provider implementations moved to `integrations-public`:

- `src/Incursa.Integrations.WorkOS/`
- `src/Incursa.Integrations.WorkOS.Abstractions/`
- `src/Incursa.Integrations.WorkOS.Access/`
- `src/Incursa.Integrations.WorkOS.AspNetCore/`
- `src/Incursa.Integrations.WorkOS.Audit/`
- `src/Incursa.Integrations.WorkOS.Webhooks/`
- `tests/Incursa.Integrations.WorkOS.Tests/`
- `tests/Incursa.Integrations.WorkOS.Webhooks.Tests/`

## `C:\src\incursa\integrations-cloudflare`

Provider-neutral packages retained in `platform`:

- `src/Incursa.Platform.CustomDomains/`
- `src/Incursa.Platform.Dns/`

Public provider implementations moved to `integrations-public`:

- `src/Incursa.Integrations.Cloudflare/`
- `src/Incursa.Integrations.Cloudflare.CustomDomains/`
- `src/Incursa.Integrations.Cloudflare.Dns/`
- `src/Incursa.Integrations.Cloudflare.KvProbe/`
- `tests/Incursa.Integrations.Cloudflare.Tests/`
- `tests/Incursa.Integrations.Cloudflare.IntegrationTests/`
- `tests/Incursa.Platform.CustomDomains.Tests/`
- `tests/Incursa.Platform.Dns.Tests/`

## `C:\src\incursa\integrations-electronicnotary`

Public provider implementations moved to `integrations-public`:

- `src/Incursa.Integrations.ElectronicNotary/`
- `src/Incursa.Integrations.ElectronicNotary.Abstractions/`
- `src/Incursa.Integrations.ElectronicNotary.Proof/`
- `src/Incursa.Integrations.ElectronicNotary.Proof.AspNetCore/`
- `tests/Incursa.Integrations.ElectronicNotary.Tests/`

## Summary

- provider-neutral capability families remain in `platform`
- public provider implementations now live in `integrations-public`
- the extracted repos still use stable package identities, even when some moved packages keep `Incursa.Platform.*` names
