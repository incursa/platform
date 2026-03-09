# Incursa.Integrations.WorkOS.AspNetCore

ASP.NET Core integration layer for Incursa WorkOS packages, including middleware and Razor widget islands.

## Install

```bash
dotnet add package Incursa.Integrations.WorkOS.AspNetCore
```

## WorkOS Widgets (Razor Tag Helpers)

Detailed handoff runbook:

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

Add tag helpers to Razor:

```cshtml
@addTagHelper *, Incursa.Integrations.WorkOS.AspNetCore
```

Include assets:

```cshtml
<workos-widgets-assets include-js="false" include-css="true" />
<workos-widgets-assets include-css="false" include-js="true" />
```

Render widgets:

```cshtml
<workos-user-management />
<workos-user-profile />
<workos-user-sessions />
<workos-user-security />
<workos-api-keys />
<workos-pipes />
<workos-admin-portal-domain-verification />
<workos-admin-portal-sso-connection />
<workos-organization-switcher />
```

### Session ID resolution

`workos-user-sessions` resolves `current-session-id` in this order:

1. Explicit `current-session-id` attribute.
2. `IWorkOsCurrentSessionIdResolver` from DI.

Default resolver reads current principal claims: `sid`, `session_id`, `workos_session_id`.

### Styling customization

Per-widget:

```cshtml
<workos-user-management
  theme-json='{"appearance":"light"}'
  elements-json='{"card":{"borderRadius":"12px"}}' />
```

Localization (optional):

```cshtml
<workos-user-profile locale="ja" text-direction="ltr" />
```

Dialog layering override (if host app has high z-index chrome):

```cshtml
<workos-user-profile dialog-z-index="10000" />
```

Global defaults (`WorkOsWidgetsOptions`):

```json
{
  "WorkOS": {
    "Widgets": {
      "ThemeJson": "{\"appearance\":\"light\"}",
      "ElementsJson": "{\"card\":{\"borderRadius\":\"12px\"}}",
      "DialogZIndex": 10000
    }
  }
}
```

### Security hardening (recommended for inline widget tokens)

Use `no-store` caching for pages hosting widgets:

```csharp
using Incursa.Integrations.WorkOS.AspNetCore.DependencyInjection;

app.UseWorkOsWidgetNoStoreResponses("/account");
```

Example CSP baseline (tighten for your app):

```csharp
app.Use(async (context, next) =>
{
    context.Response.Headers["Content-Security-Policy"] =
        "default-src 'self'; " +
        "script-src 'self'; " +
        "style-src 'self' 'unsafe-inline'; " +
        "img-src 'self' data: https:; " +
        "connect-src 'self' https://api.workos.com; " +
        "frame-ancestors 'none';";

    await next();
});
```

Redact JWT-like values before logging raw HTML/config payloads:

```csharp
using Incursa.Integrations.WorkOS.AspNetCore.Security;

var safe = WorkOsLogRedaction.RedactJwtLikeTokens(rawLogValue);
logger.LogInformation("Widget payload: {Payload}", safe);
```

## Highlights

- Dependency injection registration extensions for WorkOS services.
- Request pipeline helpers for API key authentication and principal enrichment.
- Razor tag helpers for WorkOS widget islands with server-side token issuance.
- Built-in organization switcher redirect template support.

## Target Framework

- `net10.0`
