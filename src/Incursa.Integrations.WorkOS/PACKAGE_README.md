# Incursa.Integrations.WorkOS

`Incursa.Integrations.WorkOS` is the main layer 1 WorkOS runtime package for non-ASP.NET hosts.

It sits alongside the layer 2 capability libraries in this monorepo. Use this package when you need vendor-specific WorkOS behavior that does not belong in a provider-neutral capability such as `Incursa.Platform.Access` or `Incursa.Platform.Webhooks`.

## What It Owns

- WorkOS-specific runtime clients and orchestration
- API key and management-oriented integration services
- claim enrichment and provider-side permission/scope mapping helpers
- non-host-specific webhook, token, and management support that belongs to the WorkOS adapter family

## What It Does Not Own

- the canonical access model, role registry, or effective-access rules
- ASP.NET Core request pipeline integration and Razor widget hosting
- provider-neutral webhook ingestion contracts

## Related Packages

- `Incursa.Platform.Access`: local source-of-truth access capability
- `Incursa.Integrations.WorkOS.Access`: maps WorkOS organizations and memberships into the access capability
- `Incursa.Integrations.WorkOS.AspNetCore`: ASP.NET Core middleware, request integration, and widgets
- `Incursa.Integrations.WorkOS.Webhooks`: WorkOS adapter over the shared webhook capability
- `Incursa.Integrations.WorkOS.Audit`: asynchronous WorkOS audit sink

## Install

```bash
dotnet add package Incursa.Integrations.WorkOS
```

## Best Fit

Use this package for worker services, background processors, or other non-HTTP hosts that need WorkOS-specific integration behavior without pulling in the ASP.NET Core surface.

## Credentials

Depending on the features you use, the package can participate in:

- WorkOS Management API key flows
- OIDC application credentials for interactive sign-in scenarios
- OAuth client-credentials flows for machine-to-machine access

## Target Framework

- `net10.0`
