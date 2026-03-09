# Incursa.Platform

`Incursa.Platform` is the foundational package family for the monorepo. It provides the core runtime building blocks that the higher-level capability packages build on.

## What This Package Family Is For

Use `Incursa.Platform` when you need the base infrastructure pieces that sit underneath storage-backed workflows, inbox/outbox processing, orchestration, and shared platform runtime concerns.

This package is intentionally foundational. More opinionated domain capabilities live in sibling packages such as:

- `Incursa.Platform.Access`
- `Incursa.Platform.Storage`
- `Incursa.Platform.Webhooks`
- `Incursa.Platform.Email`
- `Incursa.Platform.Operations`

## What It Owns

- base orchestration and platform runtime primitives
- shared registration helpers for the core platform runtime
- support for inbox and outbox style processing
- one-time execution and startup coordination helpers

## What It Does Not Own

- vendor-specific integrations
- provider-neutral business capability models like access or DNS
- application-specific domain logic

## Related Packages

- `Incursa.Platform.SqlServer` for SQL Server-backed infrastructure and runtime hosting
- `Incursa.Platform.Postgres` for PostgreSQL-backed infrastructure
- `Incursa.Platform.Storage` for provider-neutral storage contracts
- `Incursa.Platform.Observability` for shared observability conventions

## Typical Use

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSqlPlatform(
    "Server=localhost;Database=MyApp;Trusted_Connection=true;",
    options =>
    {
        options.EnableSchemaDeployment = true;
        options.EnableSchedulerWorkers = true;
    });
```

## Documentation

- `docs/architecture/monorepo.md`
- `docs/outbox-quickstart.md`
- `docs/inbox-quickstart.md`
