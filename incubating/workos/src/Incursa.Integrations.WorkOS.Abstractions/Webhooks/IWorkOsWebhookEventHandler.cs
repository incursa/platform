namespace Incursa.Integrations.WorkOS.Abstractions.Webhooks;

public interface IWorkOsWebhookEventHandler
{
    ValueTask HandleAsync(WorkOsWebhookEvent webhookEvent, CancellationToken ct = default);
}

