# Incursa.Platform.Health

`Incursa.Platform.Health` provides the shared health model used across platform subsystems and host integrations.

## What It Owns

- standardized health bucket and status contracts
- registration helpers for contributing subsystem health
- shared conventions for composing health signals into a service-level view

## What It Does Not Own

- ASP.NET Core endpoint hosting
- vendor-specific probes
- infrastructure-specific diagnostics

## Related Packages

- `Incursa.Platform.Health.AspNetCore` for HTTP endpoint integration
- `Incursa.Platform.HealthProbe` for probe execution helpers
- `Incursa.Platform.Observability` for adjacent metrics and event conventions
