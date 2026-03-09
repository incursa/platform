# Imported Integrations

This repository now preserves the provenance and landing zone of the local sibling integration repositories that were inspected during the monorepo cleanup.

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
- package/namespace identity was normalized to the `Incursa.Platform.Email*` family
- the imported email test slice was rewritten to the repo’s xUnit-based test conventions

## `C:\src\incursa\integrations-workos`

Decision:

- keep the existing public `src/Incursa.Platform.Audit.WorkOS/` package
- promote the provider-neutral access capability plus a focused WorkOS access adapter into `src/`
- defer broader WorkOS promotion until the remaining slices can hang cleanly off existing capability families
- preserve the broader imported WorkOS repository under `incubating/` until the remaining surfaces are split into capability-specific public packages

Result:

- public packages added:
  - `src/Incursa.Platform.Access/`
  - `src/Incursa.Platform.Access.WorkOS/`
- public package retained:
  - `src/Incursa.Platform.Audit.WorkOS/`
- public tests added:
  - `tests/Incursa.Platform.Access.Tests/`
- preserved import: `incubating/workos/`

Why incubated:

- the imported repo is still a broad vendor surface spanning auth, webhook, widget, claims, and management concerns
- only the organization-membership-to-access slice was cleanly expressible as a layer 1 adapter under a public layer 2 access capability
- the next credible promotions are likely `Incursa.Platform.Access.AspNetCore` and `Incursa.Platform.Webhooks.WorkOS`, not a new standalone identity core

## `C:\src\incursa\integrations-cloudflare`

Decision:

- extract the provider-neutral DNS capability plus a focused Cloudflare DNS adapter into `src/`
- extract the provider-neutral custom-domain capability plus a focused Cloudflare custom-hostname adapter into `src/`
- preserve the broader repository under `incubating/` instead of promoting the full vendor bucket directly

Result:

- public packages added:
  - `src/Incursa.Platform.CustomDomains/`
  - `src/Incursa.Platform.CustomDomains.Cloudflare/`
  - `src/Incursa.Platform.Dns/`
  - `src/Incursa.Platform.Dns.Cloudflare/`
- public tests added:
  - `tests/Incursa.Platform.CustomDomains.Tests/`
  - `tests/Incursa.Platform.Dns.Tests/`
- preserved import: `incubating/cloudflare/`

Why incubated:

- the current code is still a mixed vendor bucket covering storage, probing, KV, R2, load-balancing, and broader Cloudflare registration concerns
- the DNS zone/record slice and the managed custom-hostname slice were cleanly expressible as layer 1 adapters under public layer 2 capabilities

## `C:\src\incursa\integrations-electronicnotary`

Decision:

- preserve the repository under `incubating/`

Result:

- preserved import: `incubating/electronicnotary/`

Why incubated:

- the current surface mixes provider-facing code with workflow/healing/orchestration behavior
- it is not yet a clean public infrastructure package boundary

## Summary

- no imported repository was deleted
- the email/Postmark family was promoted into the public monorepo
- WorkOS now has a public audit slice plus a focused access adapter, with the broader vendor surface still incubated
- Cloudflare now has public DNS and custom-domain slices, with the broader vendor surface still incubated
- electronic-notary code was preserved under `incubating/` for later refactoring or extraction
