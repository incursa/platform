# Integration Library Standard Alignment

This repository aligns to `C:\src\incursa\llm\integration-library-standards.md` with the following mapping.

## Package and Layer Mapping

- `Incursa.Integrations.WorkOS.Abstractions`: shared contracts and option DTOs.
- `Incursa.Integrations.WorkOS`: provider package containing Layer 1 client/runtime internals plus Layer 2 capability adapters and durability-facing services.
- `Incursa.Integrations.WorkOS.AspNetCore`: ASP.NET Core hosting glue (middleware/endpoints).

## Standards Applied in This Change

1. Clean-break package consolidation completed.
   - Removed `Incursa.Integrations.WorkOS.Core`.
   - Removed `Incursa.Integrations.WorkOS.Persistence`.
   - Removed `Incursa.Integrations.WorkOS.AppAuth.Abstractions`.
   - Removed `Incursa.Integrations.WorkOS.AppAuth.AspNetCore`.
   - Their code is now compiled into `Incursa.Integrations.WorkOS` and `Incursa.Integrations.WorkOS.AspNetCore` directly.
2. Capability variance primitives added to abstractions.
   - `CapabilityNotSupportedException`
   - `IWorkOsIntegrationHandle`
   - `Supports<T>`, `TryGet<T>`, and `GetRequired<T>()` extensions
