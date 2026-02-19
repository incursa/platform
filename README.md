# Incursa Platform

.NET 10 platform for SQL-backed distributed work-queue primitives (outbox, inbox, schedulers, fanout, leases) with claim-ack-abandon semantics and database-authoritative timing.

## What you get

Incursa Platform provides durable background processing and coordination primitives that are safe to use in multi-node services:

- Outbox and inbox for reliable publishing and idempotent consumption.
- One-time and recurring scheduling with database-authoritative timing.
- Fanout/join coordination built on the same work-queue model.
- Leases and semaphores for distributed locking.
- Consistent observability, audit, and operations tracking.

## Providers

Choose a storage provider (or use InMemory for tests/dev):

- `Incursa.Platform.SqlServer`
- `Incursa.Platform.Postgres`
- `Incursa.Platform.InMemory`

Providers can auto-deploy schema (recommended for local/dev) or you can run scripts manually.

## Quick start (SQL Server)

Install:

```bash
dotnet add package Incursa.Platform
dotnet add package Incursa.Platform.SqlServer
```

Register the full platform stack:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSqlPlatform(
    "Server=localhost;Database=MyApp;Trusted_Connection=true;",
    options =>
    {
        options.EnableSchemaDeployment = true;
        options.EnableSchedulerWorkers = true;
    });

var app = builder.Build();
```

Postgres uses `AddPostgresPlatform` with the same options and tuning hooks.

## Discovery-based multi-database registration

```csharp
builder.Services.AddSingleton<IPlatformDatabaseDiscovery>(new MyTenantDiscovery());

builder.Services.AddSqlPlatformMultiDatabaseWithDiscovery(enableSchemaDeployment: true);
```

## Package map

Core and providers:

- `Incursa.Platform` (core abstractions and orchestration)
- `Incursa.Platform.SqlServer`, `Incursa.Platform.Postgres` (storage providers)
- `Incursa.Platform.InMemory` (test/dev provider)

Platform capabilities:

- `Incursa.Platform.Audit` (immutable audit timeline)
- `Incursa.Platform.Operations` (long-running operations tracking)
- `Incursa.Platform.Observability` (shared conventions and emitters)
- `Incursa.Platform.Idempotency` (TryBegin/Complete/Fail guard)
- `Incursa.Platform.ExactlyOnce` (best-effort exactly-once workflow)
- `Incursa.Platform.Email` + `Incursa.Platform.Email.Postmark` + `Incursa.Platform.Email.AspNetCore`
- `Incursa.Platform.Webhooks` + `Incursa.Platform.Webhooks.AspNetCore`
- `Incursa.Platform.Modularity` + `Incursa.Platform.Modularity.AspNetCore` + `Incursa.Platform.Modularity.Razor`
- `Incursa.Platform.Metrics.AspNetCore`, `Incursa.Platform.Metrics.HttpServer`
- `Incursa.Platform.Correlation`
- `Incursa.Platform.HealthProbe`

## Database schema

SQL Server artifacts live in `src/Incursa.Platform.SqlServer/Database/`. Use provider options to auto-deploy, or run scripts manually in controlled environments.

## Documentation

Start here:

- `docs/INDEX.md` (documentation index)
- `docs/GETTING_STARTED.md` (getting started guide)
- `docs/outbox-quickstart.md` and `docs/inbox-quickstart.md`
- `docs/observability/README.md`
- `docs/testing/README.md`

Package-specific READMEs live under `src/Incursa.Platform.*`.

## Tests and smoke app

- Tests live in `tests/Incursa.Platform.Tests/` and related projects.
- `tests/Incursa.Platform.SmokeWeb/` is a minimal ASP.NET Core UI for exercising outbox/inbox/scheduler/fanout/leases.
- `tests/Incursa.Platform.Smoke.AppHost/` is an Aspire app host that can spin up SQL Server and Postgres containers.

## Contributing

See `CONTRIBUTING.md` for development workflow and guidelines.
