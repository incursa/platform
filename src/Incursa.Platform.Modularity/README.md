# Incursa.Platform.Modularity

`Incursa.Platform.Modularity` provides engine-first module infrastructure for transport-agnostic UI, webhook, and background workflows.

## What It Owns

- module registration and discovery
- engine descriptors and manifests
- shared composition patterns for UI, webhook, and background engines
- module-level configuration and health integration points

## What It Does Not Own

- a single hosting model
- vendor-specific transport adapters
- application-specific module catalogs

## Related Packages

- `Incursa.Platform.Modularity.AspNetCore` for ASP.NET Core hosting
- `Incursa.Platform.Modularity.Razor` for Razor integration
- `Incursa.Platform.Webhooks` when module engines need provider-agnostic webhook flows

## Usage

```csharp
ModuleRegistry.RegisterModule<MyModule>();

builder.Services.AddModuleServices(builder.Configuration);
```
