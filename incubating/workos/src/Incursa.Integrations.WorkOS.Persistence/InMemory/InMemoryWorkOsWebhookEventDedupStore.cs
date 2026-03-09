namespace Incursa.Integrations.WorkOS.Persistence.InMemory;

using Incursa.Integrations.WorkOS.Abstractions.Persistence;

public sealed class InMemoryWorkOsWebhookEventDedupStore : IWorkOsWebhookEventDedupStore
{
    private readonly Dictionary<string, DateTimeOffset> _eventSeenUtc = new(StringComparer.Ordinal);
    private readonly Lock _gate = new();

    public ValueTask<bool> TryAcquireAsync(string eventId, DateTimeOffset seenUtc, TimeSpan ttl, CancellationToken ct = default)
    {
        lock (_gate)
        {
            CleanupExpired(seenUtc, ttl);
            if (_eventSeenUtc.ContainsKey(eventId))
            {
                return ValueTask.FromResult(false);
            }

            _eventSeenUtc[eventId] = seenUtc;
            return ValueTask.FromResult(true);
        }
    }

    private void CleanupExpired(DateTimeOffset nowUtc, TimeSpan ttl)
    {
        if (ttl <= TimeSpan.Zero)
        {
            return;
        }

        var cutoff = nowUtc - ttl;
        foreach (var key in _eventSeenUtc.Where(x => x.Value < cutoff).Select(static x => x.Key).ToArray())
        {
            _eventSeenUtc.Remove(key);
        }
    }
}

