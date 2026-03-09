# Incursa.Integrations.WorkOS.Abstractions

This folder contains the shared contract package for the WorkOS integration family.

Use it when you are extending the WorkOS adapters themselves or wiring custom implementations into the WorkOS runtime or ASP.NET Core packages.

## Where It Fits

- below the provider-neutral `Incursa.Platform.*` capabilities
- below application code that wants a stable vendor-specific extension point
- above the concrete WorkOS runtime and ASP.NET Core packages

## Common Consumers

- `Incursa.Integrations.WorkOS`
- `Incursa.Integrations.WorkOS.AspNetCore`
- `Incursa.Integrations.WorkOS.Audit`
- `Incursa.Integrations.WorkOS.Webhooks`

## See Also

- `PACKAGE_README.md` for the NuGet package overview
- `../Incursa.Integrations.WorkOS/README.md`
