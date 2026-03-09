# Incursa.Integrations.WorkOS.Abstractions

Shared contracts and option models for building WorkOS integrations in Incursa services.

## Install

```bash
dotnet add package Incursa.Integrations.WorkOS.Abstractions
```

## Highlights

- Provider-agnostic interfaces for API key identity and permission mapping.
- Contracts for organization-to-tenant resolution and management access checks.
- Webhook processing abstractions with idempotency extension points.
- Audit event ingestion abstractions:
  - `IWorkOsAuditClient`
  - `WorkOsAuditCreateEventRequest`
  - `WorkOsAuditActor`, `WorkOsAuditTarget`, `WorkOsAuditContext`
- Integration capability helpers (`IWorkOsIntegrationHandle`, `Supports`, `TryGet`, `GetRequired`).
- Widget integration contracts:
  - `IWorkOsWidgetIdentityResolver`
  - `IWorkOsCurrentSessionIdResolver`
  - `WorkOsWidgetIdentity`
  - `WorkOsWidgetType`
  - `WorkOsWidgetsOptions`

## Target Framework

- `net10.0`
