# Platform Scope Boundary

This document defines what belongs in the `platform` repository versus provider-specific integration repositories.

## Boundary Decision

- `platform` owns provider-agnostic distributed systems primitives and reusable framework contracts.
- `integrations-*` repositories own provider-specific adapters and operational workflows.
- External-provider integration packages are released independently from `platform`.

## In Scope For `platform`

- Work-queue primitives (`outbox`, `inbox`, `scheduler`, `fanout`, `leases`) and core orchestration.
- Storage providers (`SqlServer`, `Postgres`, `InMemory`) for core primitives.
- Shared cross-cutting libraries (`Observability`, `Correlation`, `Audit`, `Operations`, `Idempotency`, `ExactlyOnce`).
- Provider-agnostic integration frameworks such as `Email` and `Webhooks` contracts.
- Exception: existing reference/default adapters tightly coupled to platform contracts (currently `Incursa.Platform.Email.Postmark`).

## Out Of Scope For `platform`

- New external provider product integrations (for example WorkOS, Cloudflare, electronic notary providers).
- Provider-specific domain workflows that can evolve independently from platform primitives.
- Provider libraries requiring separate release cadence or risk isolation.

## Rules For New Work

1. New external provider integration goes to a dedicated `integrations-<provider>` repository by default.
2. `platform` may only add provider-specific code when it is an approved reference adapter.
3. Approved reference adapters must be explicitly listed in `scripts/quality/platform-scope.rules.json`.
4. Any boundary exception requires updating this doc and the scope rules in the same PR.

## Current Portfolio Mapping

- `platform`: platform primitives and reusable contracts.
- `integrations-workos`: WorkOS auth, webhook, and management integration surface.
- `integrations-cloudflare`: Cloudflare KV/R2/custom-hostname/load-balancer integrations.
- `integrations-electronicnotary`: electronic notary domain contracts and proof integration.
- `integrations-postmark`: reserved for future standalone packaging; authoritative Postmark adapter remains in `platform`.
