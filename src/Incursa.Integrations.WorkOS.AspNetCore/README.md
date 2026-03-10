# Incursa.Integrations.WorkOS.AspNetCore

This folder contains the ASP.NET Core-facing WorkOS integration package.

It is the place to look when you need WorkOS-specific request middleware, organization-selection behavior, or widget hosting. If you want provider-neutral access context instead, start with `Incursa.Platform.Access.AspNetCore`.

## Use This Package For

- WorkOS middleware and principal enrichment
- organization switcher and organization-context behavior
- widget tag helpers and supporting assets
- WorkOS-specific host wiring that should not be promoted into a provider-neutral capability
- WorkOS-specific `ClaimsPrincipal` creation for the provider-neutral access session model

## Use Another Package For

- `Incursa.Platform.Access.AspNetCore`: provider-neutral current access context
- `Incursa.Integrations.WorkOS.Access`: synchronizing WorkOS organizations and memberships into the access capability
- `Incursa.Integrations.WorkOS.Webhooks`: WorkOS webhook adapter over the shared webhook capability

## See Also

- `PACKAGE_README.md` for the NuGet package overview
- `../Incursa.Integrations.WorkOS/README.md`

## Custom UI ASP.NET Core Wiring

When an app owns its own login UI but still wants WorkOS-backed authentication, this package provides the WorkOS-specific ASP.NET bridge:

```csharp
services.AddWorkOsAccessAspNetCore(
    configureAccess: options =>
    {
        options.ScopeRootExternalLinkProvider = "workos";
        options.ScopeRootExternalLinkResourceType = "organization";
    },
    configureSessionCookies: options =>
    {
        options.AuthenticationScheme = "Access";
    });
```

That registration keeps the app-facing contracts provider-neutral while replacing the default claims-principal factory with a WorkOS-aware one.
