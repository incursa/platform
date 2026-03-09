# WorkOS AppAuth ASP.NET Core Source Folder

This is a source folder compiled into `Incursa.Integrations.WorkOS.AspNetCore`. It is not a separately published package.

## What Lives Here

- normalized WorkOS claim parsing for ASP.NET Core requests
- dynamic permission-policy wiring
- organization-context middleware and selection-store support
- membership verification hooks and higher-level registration helpers

## Why It Is A Source Folder

These pieces are implementation support for the WorkOS ASP.NET Core package. They are intentionally vendor-specific and tightly coupled to the ASP.NET Core host surface.

## Related Package

- `Incursa.Integrations.WorkOS.AspNetCore`
