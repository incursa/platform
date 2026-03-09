# Incursa.Integrations.WorkOS.Abstractions

`Incursa.Integrations.WorkOS.Abstractions` contains the shared contracts, option models, and helper types used across the WorkOS layer 1 package family.

## What It Owns

- vendor-facing interfaces for WorkOS identity, management, and integration handles
- option models shared by runtime, ASP.NET Core, and capability-specific WorkOS packages
- audit- and webhook-related WorkOS contracts that are still intentionally vendor-specific
- widget contracts and session-resolution contracts shared by the WorkOS ASP.NET Core package

## What It Does Not Own

- provider-neutral capability contracts
- the canonical access model or role/permission registry
- concrete HTTP clients, middleware, or storage implementations

## Install

```bash
dotnet add package Incursa.Integrations.WorkOS.Abstractions
```

## Typical Use

Most consumers do not install this package directly unless they are extending or composing the WorkOS integration family. Application code usually depends on one of the higher-level packages instead.

## Highlights

- API key identity and permission-mapping interfaces
- organization-to-tenant resolution and management access-check contracts
- webhook extension points and dedupe/idempotency hooks
- audit event ingestion contracts
- widget identity and current-session resolution contracts

## Target Framework

- `net10.0`
