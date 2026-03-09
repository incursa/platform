namespace Incursa.Integrations.WorkOS.Abstractions.Persistence;

public interface IWorkOsWebhookEventDedupStore
{
    ValueTask<bool> TryAcquireAsync(string eventId, DateTimeOffset seenUtc, TimeSpan ttl, CancellationToken ct = default);
}

