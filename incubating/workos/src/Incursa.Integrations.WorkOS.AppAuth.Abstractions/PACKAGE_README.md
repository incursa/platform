# AppAuth Abstractions Source Folder

This is a source folder compiled into `Incursa.Integrations.WorkOS` (not a standalone package).

Contents:

- claim normalization (`IWorkOsClaimsAccessor`)
- organization selection/context (`IOrganizationContextAccessor`)
- app auth configuration (`WorkOsAppAuthOptions`)

`HttpContext`-bound contracts live in the ASP.NET Core source folder and compile into `Incursa.Integrations.WorkOS.AspNetCore`.
