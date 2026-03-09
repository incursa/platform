# Incursa.Integrations.WorkOS.AspNetCore

This folder contains the ASP.NET Core-facing WorkOS integration package.

It is the place to look when you need WorkOS-specific request middleware, organization-selection behavior, or widget hosting. If you want provider-neutral access context instead, start with `Incursa.Platform.Access.AspNetCore`.

## Use This Package For

- WorkOS middleware and principal enrichment
- organization switcher and organization-context behavior
- widget tag helpers and supporting assets
- WorkOS-specific host wiring that should not be promoted into a provider-neutral capability

## Use Another Package For

- `Incursa.Platform.Access.AspNetCore`: provider-neutral current access context
- `Incursa.Integrations.WorkOS.Access`: synchronizing WorkOS organizations and memberships into the access capability
- `Incursa.Integrations.WorkOS.Webhooks`: WorkOS webhook adapter over the shared webhook capability

## See Also

- `PACKAGE_README.md` for the NuGet package overview
- `../Incursa.Integrations.WorkOS/README.md`
