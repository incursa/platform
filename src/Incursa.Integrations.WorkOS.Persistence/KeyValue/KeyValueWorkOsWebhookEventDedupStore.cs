namespace Incursa.Integrations.WorkOS.Persistence.KeyValue;

using Incursa.Integrations.WorkOS.Abstractions.Persistence;

public sealed class KeyValueWorkOsWebhookEventDedupStore : IWorkOsWebhookEventDedupStore
{
    private readonly IWorkOsKeyValueStore _store;

    public KeyValueWorkOsWebhookEventDedupStore(IWorkOsKeyValueStore store)
    {
        ArgumentNullException.ThrowIfNull(store);
        _store = store;
    }

    public async ValueTask<bool> TryAcquireAsync(string eventId, DateTimeOffset seenUtc, TimeSpan ttl, CancellationToken ct = default)
    {
        var key = "workos:webhooks:event:" + eventId;
        var existing = await _store.GetStringAsync(key, ct).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(existing))
        {
            return false;
        }

        var ttlToUse = ttl <= TimeSpan.Zero ? TimeSpan.FromHours(24) : ttl;
        await _store.SetStringAsync(key, seenUtc.ToString("O", CultureInfo.InvariantCulture), ttlToUse, ct).ConfigureAwait(false);
        return true;
    }
}

