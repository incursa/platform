# Incursa.Integrations.WorkOS

WorkOS provider package for Incursa integrations (non-ASP.NET core).

## Install

```bash
dotnet add package Incursa.Integrations.WorkOS
```

## Includes

- runtime auth and management services
- audit event client services
- webhook verification/processing services
- in-memory and key-value persistence adapters
- host-agnostic app-auth contracts

## Credential Models

- WorkOS Management API key for server-to-server management + claims enrichment.
- WorkOS OIDC app credentials for interactive login.
- WorkOS OAuth client-credentials for machine-to-machine tokens.

## Best For

- Service teams that need WorkOS provider runtime behavior without ASP.NET dependencies.
- Worker/background services using WorkOS API key, management, and webhook flows.
- Hosts that compose ASP.NET integration by adding `Incursa.Integrations.WorkOS.AspNetCore`.

## Target Framework

- `net10.0`
