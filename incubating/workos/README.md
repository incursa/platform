# Incursa WorkOS Integration

Shared WorkOS integration platform for Incursa services. It provides reusable contracts, runtime auth, management key lifecycle APIs, webhook verification/processing, and pluggable persistence providers.

## Credential Models

The library supports three WorkOS credential uses:

- WorkOS Management API key (`ApiKey`) for server-to-server calls.
- WorkOS OIDC app credentials (`Authority`, `ClientId`, `ClientSecret`) for user sign-in.
- WorkOS OAuth client credentials for machine-to-machine token acquisition.

## Layout

- `src/Incursa.Integrations.WorkOS.Abstractions`
- `src/Incursa.Integrations.WorkOS`
- `src/Incursa.Integrations.WorkOS.AspNetCore`
- `tests/`

## Build

```bash
dotnet restore Incursa.Integrations.WorkOS.slnx
dotnet build Incursa.Integrations.WorkOS.slnx -c Release
```

## Test

```bash
dotnet test --solution Incursa.Integrations.WorkOS.slnx -c Release
```

## In-Memory Platform (Unit Tests)

Use the in-memory platform layer to run deterministic unit tests without real WorkOS APIs:

```csharp
services.AddWorkOsInMemoryIntegration(
    configureOptions: options => { options.StrictPermissionMapping = false; },
    seed: state =>
    {
        state.SeedTenantMapping("org_1", "tenant-1");
        state.SeedOrganizationAdmin("org_1", "user_admin");
    });
```

## Pack (local)

```powershell
pwsh -File scripts/pack.ps1
```

## Scope

Scope policy: `docs/quality/repo-scope-boundary.md`.
