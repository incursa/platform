# Incursa.Platform.Observability.Analyzers

`Incursa.Platform.Observability.Analyzers` is a Roslyn analyzer package that enforces Incursa audit and observability conventions at compile time.

## Install

```bash
dotnet add package Incursa.Platform.Observability.Analyzers
```

## Included Rules

- `OBS001`: Audit event names should be lowercase and dot-separated.

## Why Use It

Use this analyzer package to prevent convention drift and catch telemetry naming issues during local development and CI builds.

## Build and Pack

```powershell
dotnet pack ./tools/observability/src/Incursa.Platform.Observability.Analyzers/Incursa.Platform.Observability.Analyzers.csproj -c Release -o ./nupkgs
```

## Repository

- https://github.com/incursa/platform
