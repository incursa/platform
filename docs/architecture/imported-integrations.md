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
- preserve the broader imported WorkOS repository under `incubating/` until it is split into capability-specific public packages

Result:

- public package retained: `src/Incursa.Platform.Audit.WorkOS/`
- preserved import: `incubating/workos/`

Why incubated:

- the imported repo is a broad vendor surface spanning auth, webhook, widget, and management concerns
- it is not yet a clean capability-based public package family

## `C:\src\incursa\integrations-cloudflare`

Decision:

- preserve the repository under `incubating/` instead of promoting it directly into `src/`

Result:

- preserved import: `incubating/cloudflare/`

Why incubated:

- the current code is a mixed vendor bucket covering multiple concerns such as storage, hostnames, probing, and load-balancing
- it should be split by capability before any public packaging decision is made

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
- WorkOS was kept public only where the boundary was already clear (`Incursa.Platform.Audit.WorkOS`)
- Cloudflare and electronic-notary code were preserved under `incubating/` for later refactoring or extraction
