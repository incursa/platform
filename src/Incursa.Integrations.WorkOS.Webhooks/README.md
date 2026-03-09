# Incursa.Integrations.WorkOS.Webhooks

`Incursa.Integrations.WorkOS.Webhooks` adds a thin WorkOS provider adapter on top of `Incursa.Platform.Webhooks`.

It exists so WorkOS webhook signature validation and event classification can plug into the shared webhook pipeline without reintroducing a parallel WorkOS-owned endpoint model.

## Install

```bash
dotnet add package Incursa.Integrations.WorkOS.Webhooks
```

## What You Get

- WorkOS signature validation through the existing webhook authenticator pipeline
- WorkOS payload classification into provider event id, event type, dedupe key, and partition key
- Service registration for a WorkOS webhook provider without introducing a parallel endpoint or processor model

## What It Does Not Own

- the provider-neutral webhook ingestion contracts
- application-specific event handlers
- broader WorkOS middleware or request-context behavior

## Typical Use

```csharp
services.AddIncursaWebhooks();

services.AddWorkOsWebhooks(options =>
{
    options.SigningSecret = configuration["WorkOS:WebhookSigningSecret"]!;
});
```

Register `IWorkOsWebhookHandler` implementations when you want WorkOS events to be processed by the shared webhook processor.

## Related Packages

- `Incursa.Platform.Webhooks`
- `Incursa.Platform.Webhooks.AspNetCore`
- `Incursa.Integrations.WorkOS`
