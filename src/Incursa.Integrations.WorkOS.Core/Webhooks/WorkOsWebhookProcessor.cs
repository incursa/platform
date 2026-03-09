namespace Incursa.Integrations.WorkOS.Core.Webhooks;

using Incursa.Integrations.WorkOS.Abstractions.Persistence;
using Incursa.Integrations.WorkOS.Abstractions.Telemetry;
using Incursa.Integrations.WorkOS.Abstractions.Webhooks;

public sealed class WorkOsWebhookProcessor : IWorkOsWebhookProcessor
{
    private readonly IWorkOsWebhookEventDedupStore _dedupeStore;
    private readonly IReadOnlyCollection<IWorkOsWebhookEventHandler> _handlers;
    private readonly IWorkOsIntegrationTelemetry _telemetry;
    private readonly TimeSpan _dedupeTtl;

    public WorkOsWebhookProcessor(
        IWorkOsWebhookEventDedupStore dedupeStore,
        IEnumerable<IWorkOsWebhookEventHandler> handlers,
        TimeSpan dedupeTtl,
        IWorkOsIntegrationTelemetry? telemetry = null)
    {
        ArgumentNullException.ThrowIfNull(dedupeStore);
        ArgumentNullException.ThrowIfNull(handlers);

        _dedupeStore = dedupeStore;
        _handlers = handlers.ToArray();
        _dedupeTtl = dedupeTtl <= TimeSpan.Zero ? TimeSpan.FromHours(24) : dedupeTtl;
        _telemetry = telemetry ?? NullWorkOsIntegrationTelemetry.Instance;
    }

    public async ValueTask<WorkOsWebhookProcessResult> ProcessAsync(WorkOsWebhookEvent webhookEvent, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(webhookEvent);

        var acquired = await _dedupeStore.TryAcquireAsync(webhookEvent.EventId, DateTimeOffset.UtcNow, _dedupeTtl, ct).ConfigureAwait(false);
        if (!acquired)
        {
            _telemetry.WebhookProcessed(webhookEvent.EventType, webhookEvent.EventId, processed: false, duplicate: true, failureReason: null);
            return new WorkOsWebhookProcessResult(false, true, "duplicate_event");
        }

        foreach (var handler in _handlers)
        {
            await handler.HandleAsync(webhookEvent, ct).ConfigureAwait(false);
        }

        _telemetry.WebhookProcessed(webhookEvent.EventType, webhookEvent.EventId, processed: true, duplicate: false, failureReason: null);
        return new WorkOsWebhookProcessResult(true, false, null);
    }
}

