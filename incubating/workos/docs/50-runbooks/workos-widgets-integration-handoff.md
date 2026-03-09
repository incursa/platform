---
workbench:
  type: runbook
  workItems: []
  codeRefs:
    - "C:/src/incursa/integrations-workos/src/Incursa.Integrations.WorkOS.AspNetCore/WorkOsIntegrationServiceCollectionExtensions.cs"
    - "C:/src/incursa/integrations-workos/src/Incursa.Integrations.WorkOS.AspNetCore/DependencyInjection/WorkOsWidgetsServiceCollectionExtensions.cs"
    - "C:/src/incursa/integrations-workos/src/Incursa.Integrations.WorkOS.AspNetCore/Widgets/TagHelpers"
    - "C:/src/incursa/integrations-workos/src/Incursa.Integrations.WorkOS.Abstractions/Configuration/WorkOsWidgetsOptions.cs"
    - "C:/src/incursa/integrations-workos/src/Incursa.Integrations.WorkOS.Abstractions/Widgets"
  pathHistory:
    - "C:/docs/50-runbooks/workos-widgets-integration-handoff.md"
  path: /docs/50-runbooks/workos-widgets-integration-handoff.md
related: []
---

# WorkOS Widgets Integration Handoff (Razor Pages)

This runbook is designed for handoff to another AI or engineer integrating the WorkOS widget library into a new ASP.NET Core Razor Pages app.

## Goal

Integrate `Incursa.Integrations.WorkOS.AspNetCore` so the app can render WorkOS widget islands with:

- Server-side token issuance.
- Razor tag helpers.
- Optional session ID auto-resolution for `workos-user-sessions`.
- Styling customization through WorkOS `theme` and `elements`.

## Prerequisites

- Target app uses ASP.NET Core Razor Pages.
- WorkOS dashboard is configured for widgets.
- App origin is listed in WorkOS **Allowed Web Origins** (exact scheme + host + port).
- Host app can resolve current WorkOS org/user identity.

## Quick Start (Copy/Paste)

### 1) Add package

```bash
dotnet add package Incursa.Integrations.WorkOS.AspNetCore
```

### 2) Register services in `Program.cs`

```csharp
using Incursa.Integrations.WorkOS;
using Incursa.Integrations.WorkOS.Abstractions.Widgets;

builder.Services.AddWorkOsWidgets(options =>
{
    builder.Configuration.GetSection("WorkOS:Widgets").Bind(options);
});

builder.Services.AddScoped<IWorkOsWidgetIdentityResolver, AppWorkOsWidgetIdentityResolver>();

// Optional: only needed if you want custom session-id logic.
// Default resolver reads claims: sid, session_id, workos_session_id.
// builder.Services.AddScoped<IWorkOsCurrentSessionIdResolver, AppCurrentSessionIdResolver>();
```

### 2.1) Add security middleware for inline widget tokens

```csharp
using Incursa.Integrations.WorkOS.AspNetCore.DependencyInjection;

// Applies no-store headers to HTML responses under /account by default.
app.UseWorkOsWidgetNoStoreResponses("/account");

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

### 3) Add tag helpers in `Pages/_ViewImports.cshtml`

```cshtml
@addTagHelper *, Incursa.Integrations.WorkOS.AspNetCore
```

### 4) Include widget assets once in layout/page sections

```cshtml
<workos-widgets-assets include-js="false" include-css="true" />
<workos-widgets-assets include-css="false" include-js="true" />
```

### 5) Render widgets in a page

```cshtml
<workos-user-profile />
<workos-user-security />
<workos-user-sessions />
```

## Required Resolver Contract

Implement `IWorkOsWidgetIdentityResolver` in the host app:

```csharp
using Incursa.Integrations.WorkOS.Abstractions.Widgets;

public sealed class AppWorkOsWidgetIdentityResolver : IWorkOsWidgetIdentityResolver
{
    public Task<WorkOsWidgetIdentity> ResolveAsync(CancellationToken cancellationToken)
    {
        // Replace with app-specific tenant/user resolution.
        return Task.FromResult(new WorkOsWidgetIdentity(
            OrganizationId: "org_...",
            UserId: "user_...",
            OrganizationExternalId: "tenant-optional"));
    }
}
```

If no resolver is registered, widget rendering fails with a clear error.

## Configuration (`appsettings.*.json`)

```json
{
  "WorkOS": {
    "Widgets": {
      "ApiKey": "sk_test_...",
      "ApiBaseUrl": "https://api.workos.com",
      "AllowAnonymousUsers": false,
      "ThemeJson": "{\"appearance\":\"light\"}",
      "ElementsJson": "{\"card\":{\"borderRadius\":\"12px\"}}",
      "Locale": "en-US",
      "TextDirection": "ltr",
      "DialogZIndex": 10000,
      "WidgetScopes": {
        "UsersManagement": [ "widgets:users-table:manage" ],
        "ApiKeys": [ "widgets:api-keys:manage" ],
        "DomainVerification": [ "widgets:domain-verification:manage" ],
        "SsoConnection": [ "widgets:sso:manage" ]
      },
      "OrganizationSwitcherDefaults": {
        "IdentifierPreference": "WorkOsId",
        "RedirectMode": "Template",
        "PreserveCurrentPath": true,
        "DefaultTemplate": "/account/settings?org={organizationId}",
        "FixedRoute": null
      }
    }
  }
}
```

## Tag Reference

Supported tags:

- `<workos-user-management />`
- `<workos-user-profile />`
- `<workos-user-sessions />`
- `<workos-user-security />`
- `<workos-api-keys />`
- `<workos-pipes />`
- `<workos-admin-portal-domain-verification />`
- `<workos-admin-portal-sso-connection />`
- `<workos-organization-switcher />`

Common optional attributes on widget tags:

- `id`
- `class`
- `auth-token` (explicit token override)
- `theme-json`
- `elements-json`
- `locale`
- `text-direction` (`ltr` or `rtl`)
- `dialog-z-index`

Organization switcher extras:

- `redirect-template`
- `redirect-fixed-route`
- `switch-endpoint`
- `create-organization-url`
- `create-organization-label`
- `create-organization-target`
- `prefer-external-id`
- `external-id-map-json`

User sessions extras:

- `current-session-id` (optional; if omitted, resolver is used)

## Session ID Behavior (`workos-user-sessions`)

Resolution order:

1. `current-session-id` tag attribute.
2. `IWorkOsCurrentSessionIdResolver` from DI.

Default package resolver checks claims in order:

- `sid`
- `session_id`
- `workos_session_id`

## Styling Customization

Per-widget:

```cshtml
<workos-user-management
    theme-json='{"appearance":"light","accentColor":"green"}'
    elements-json='{"card":{"borderRadius":"12px"}}' />
```

Dialog z-index override:

```cshtml
<workos-user-profile dialog-z-index="10000" />
```

Global defaults are configured via `WorkOS:Widgets:ThemeJson` and `WorkOS:Widgets:ElementsJson`.

## Suggested Account/Settings Layout

This widget set maps naturally to an account/settings experience.

### Recommended grouping

Core account settings section:

- `workos-user-profile`
- `workos-user-security`
- `workos-user-sessions`

Organization/admin settings section (role-gated):

- `workos-user-management`
- `workos-api-keys`
- `workos-admin-portal-domain-verification`
- `workos-admin-portal-sso-connection`
- `workos-pipes`

Top-right org/context controls:

- `workos-organization-switcher`

### Suggested page composition

In a `Pages/Account/Settings.cshtml` page:

- Render core account widgets in main content.
- Render admin widgets only when user has admin role/permission.
- Wrap widgets in existing card/panel components from the app design system.
- Include widget assets in layout sections once (not in every partial).

Example skeleton:

```cshtml
@page "/account/settings"

@section Styles {
    <workos-widgets-assets include-js="false" include-css="true" />
}

<h1>Account Settings</h1>

<section>
    <h2>Profile</h2>
    <workos-user-profile />
    <workos-user-security class="mt-3" />
    <workos-user-sessions class="mt-3" />
</section>

@if (User.IsInRole("Admin"))
{
    <section class="mt-4">
        <h2>Organization Administration</h2>
        <workos-user-management />
        <workos-api-keys class="mt-3" />
        <workos-admin-portal-domain-verification class="mt-3" />
        <workos-admin-portal-sso-connection class="mt-3" />
    </section>
}

@section Scripts {
    <workos-widgets-assets include-css="false" include-js="true" />
}
```

## Top-Right Settings Dropdown: Display Name Guidance

Use this claims fallback chain by default:

1. `name`
2. `given_name` + `family_name`
3. `email`
4. `"Account"` fallback

Razor helper example:

```cshtml
@{
    var name = User.FindFirst("name")?.Value;
    if (string.IsNullOrWhiteSpace(name))
    {
        var given = User.FindFirst("given_name")?.Value;
        var family = User.FindFirst("family_name")?.Value;
        name = string.Join(" ", new[] { given, family }.Where(static v => !string.IsNullOrWhiteSpace(v))).Trim();
    }
    if (string.IsNullOrWhiteSpace(name))
    {
        name = User.FindFirst("email")?.Value;
    }
    if (string.IsNullOrWhiteSpace(name))
    {
        name = "Account";
    }
}
```

Use this in your header dropdown label, and keep an app-level override path if your domain model has a preferred display name.

## Troubleshooting

- **CORS blocked from `api.workos.com`**:
  - Ensure exact browser origin is in WorkOS Allowed Web Origins.
- **Widget token errors**:
  - Verify `WorkOS:Widgets:ApiKey` and resolver output (`OrganizationId`, `UserId`).
- **`workos-user-sessions` not rendering**:
  - Provide `current-session-id` or register/verify `IWorkOsCurrentSessionIdResolver`.
- **Users/API keys permissions issues**:
  - Confirm token scopes and WorkOS role permissions match widget requirements.
- **Theme/elements not applying**:
  - Validate `theme-json` and `elements-json` are valid JSON strings.
- **Sensitive token values in logs**:
  - Use `WorkOsLogRedaction.RedactJwtLikeTokens(...)` before logging raw payloads.

## AI Handoff Prompt Template

Use this prompt in another project:

```text
Integrate Incursa.Integrations.WorkOS.AspNetCore widgets into this ASP.NET Core Razor Pages app.

Tasks:
1) Install package: Incursa.Integrations.WorkOS.AspNetCore.
2) In Program.cs:
   - call AddWorkOsWidgets(options => bind WorkOS:Widgets config),
   - register IWorkOsWidgetIdentityResolver implementation for current org/user,
   - optionally register IWorkOsCurrentSessionIdResolver if app has custom session-id source.
3) In Pages/_ViewImports.cshtml add:
   - @addTagHelper *, Incursa.Integrations.WorkOS.AspNetCore
4) In layout/page sections include:
   - <workos-widgets-assets include-js="false" include-css="true" />
   - <workos-widgets-assets include-css="false" include-js="true" />
5) Create/extend Account Settings page:
   - core widgets: workos-user-profile, workos-user-security, workos-user-sessions
   - admin widgets: workos-user-management, workos-api-keys, workos-admin-portal-domain-verification, workos-admin-portal-sso-connection, workos-pipes
   - include workos-organization-switcher in top-right account/context area.
6) Use claims-based display name fallback in top-right dropdown:
   - name -> given_name+family_name -> email -> "Account".
7) Add WorkOS Allowed Web Origins for local and deployed app origins.
```
