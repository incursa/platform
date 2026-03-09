# Incursa.Integrations.WorkOS.AspNetCore

`Incursa.Integrations.WorkOS.AspNetCore` is the layer 1 ASP.NET Core host package for the WorkOS family.

Use it when you need WorkOS-specific middleware, current-request integration, organization selection, or Razor widget hosting in an ASP.NET Core application.

## What It Owns

- ASP.NET Core middleware and DI registration for WorkOS-backed request handling
- request principal enrichment and organization-context helpers
- Razor tag helpers and asset helpers for WorkOS widgets
- host-side security helpers for widget pages and related logging concerns

## What It Does Not Own

- the canonical access model or effective-access rules
- the provider-neutral ASP.NET Core access context surface
- provider-neutral webhook ingestion

## Relationship To Other Packages

- use `Incursa.Platform.Access.AspNetCore` for provider-neutral request-time access context
- use `Incursa.Integrations.WorkOS.Access` when WorkOS organizations and memberships need to flow into the access capability
- use this package for the remaining vendor-specific ASP.NET Core behavior that should stay WorkOS-shaped

## Install

```bash
dotnet add package Incursa.Integrations.WorkOS.AspNetCore
```

## Widgets

Detailed widget handoff notes live in:

- `docs/50-runbooks/workos-widgets-integration-handoff.md`

Register services:

```csharp
using Incursa.Integrations.WorkOS;
using Incursa.Integrations.WorkOS.Abstractions.Widgets;

builder.Services.AddWorkOsWidgets(options =>
{
    builder.Configuration.GetSection("WorkOS:Widgets").Bind(options);
});

builder.Services.AddScoped<IWorkOsWidgetIdentityResolver, YourWidgetIdentityResolver>();
```

Add tag helpers:

```cshtml
@addTagHelper *, Incursa.Integrations.WorkOS.AspNetCore
```

## Session Resolution

`workos-user-sessions` resolves `current-session-id` in this order:

1. Explicit `current-session-id` attribute
2. `IWorkOsCurrentSessionIdResolver` from DI

The default resolver reads `sid`, `session_id`, and `workos_session_id` claims.

## Security Notes

- prefer `no-store` responses for pages that host inline widget tokens
- apply an explicit CSP suitable for the widgets you enable
- redact JWT-like payloads before logging raw widget configuration or HTML

## Target Framework

- `net10.0`
