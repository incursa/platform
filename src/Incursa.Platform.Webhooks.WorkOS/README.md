# Incursa.Platform.Webhooks.WorkOS

`Incursa.Platform.Webhooks.WorkOS` adds a thin WorkOS provider adapter on top of `Incursa.Platform.Webhooks`.

## Install

```bash
dotnet add package Incursa.Platform.Webhooks.WorkOS
```

## What You Get

- WorkOS signature validation through the existing webhook authenticator pipeline
- WorkOS payload classification into provider event id, event type, dedupe key, and partition key
- Service registration for a WorkOS webhook provider without introducing a parallel endpoint or processor model

## Typical Use

```csharp
services.AddIncursaWebhooks();

services.AddWorkOsWebhooks(options =>
{
    options.SigningSecret = configuration["WorkOS:WebhookSigningSecret"]!;
});
```

Register `IWorkOsWebhookHandler` implementations when you want WorkOS events to be processed by the shared webhook processor.
