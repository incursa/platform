# Incursa.Integrations.WorkOS

This folder contains the main WorkOS layer 1 integration package for the monorepo.

If you are browsing the repository, start here when you want the vendor-specific WorkOS family rather than the provider-neutral capabilities.

## Family Map

- `Incursa.Integrations.WorkOS`: non-ASP.NET runtime integration surface
- `Incursa.Integrations.WorkOS.Abstractions`: shared WorkOS-facing contracts and option models
- `Incursa.Integrations.WorkOS.Access`: WorkOS adapter for `Incursa.Platform.Access`
- `Incursa.Integrations.WorkOS.AspNetCore`: ASP.NET Core middleware, widgets, and request integration
- `Incursa.Integrations.WorkOS.Audit`: WorkOS sink for `Incursa.Platform.Audit`
- `Incursa.Integrations.WorkOS.Webhooks`: WorkOS adapter for `Incursa.Platform.Webhooks`

## Relationship To Layer 2

The WorkOS packages are public layer 1 vendor adapters. They do not define the canonical access or webhook models for the repository.

- Access state, role definitions, and effective-access rules live in `Incursa.Platform.Access`
- Provider-neutral webhook ingestion lives in `Incursa.Platform.Webhooks`
- WorkOS packages attach vendor behavior underneath those capability families where the boundary is clean

## See Also

- `PACKAGE_README.md` for the NuGet package overview
- `../Incursa.Platform.Access/README.md`
- `../Incursa.Platform.Webhooks/README.md`
