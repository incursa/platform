namespace Incursa.Integrations.WorkOS.Abstractions.Webhooks;

public interface IWorkOsWebhookProcessor
{
    ValueTask<WorkOsWebhookProcessResult> ProcessAsync(WorkOsWebhookEvent webhookEvent, CancellationToken ct = default);
}

