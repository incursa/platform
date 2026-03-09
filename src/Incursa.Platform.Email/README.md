# Incursa.Platform.Email

`Incursa.Platform.Email` provides provider-agnostic outbound email contracts, queueing, dispatch, idempotency, and observability primitives.

## What It Owns

- outbound email message contracts and addressing models
- queueing and dispatch abstractions for email workflows
- idempotency and retry-friendly delivery primitives
- provider-neutral delivery and processing conventions

## What It Does Not Own

- provider-specific delivery APIs
- HTTP hosting concerns
- email-template authoring systems or CMS-style content tooling

## Related Packages

- `Incursa.Platform.Email.AspNetCore` for ASP.NET Core integration
- `Incursa.Platform.Email.Postgres` and `Incursa.Platform.Email.SqlServer` for persistence-backed delivery support
- `Incursa.Platform.Email.Postmark` for a Postmark-specific adapter

## When To Start Here

Start here when your application needs reliable outbound email behavior but should remain independent of a single delivery provider.
