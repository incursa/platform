# AppAuth ASP.NET Core Source Folder

This is a source folder compiled into `Incursa.Integrations.WorkOS.AspNetCore` (not a standalone package).

Contents:

- normalized WorkOS claim parsing
- dynamic permission policies (`perm:*`, `perm:any:*`)
- organization context middleware and org selection store
- pluggable membership verification (`IWorkOsOrganizationMembershipResolver`)
- registration extension for common UI auth wiring
