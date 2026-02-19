# Incursa.Platform.Email.AspNetCore

ASP.NET Core helpers for Incursa.Platform.Email outbox integrations.

## Registrations

- `AddIncursaEmailCore` registers the core outbox and processor components.
- `AddIncursaEmailPostmark` registers the Postmark sender adapter.
- `AddIncursaEmailProcessingHostedService` runs the processor on an interval.

See `/docs/email/README.md` for architecture and quick start examples.
