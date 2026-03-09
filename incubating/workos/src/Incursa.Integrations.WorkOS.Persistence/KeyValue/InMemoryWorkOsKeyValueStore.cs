namespace Incursa.Integrations.WorkOS.Persistence.KeyValue;

public sealed class InMemoryWorkOsKeyValueStore : IWorkOsKeyValueStore
{
    private readonly Dictionary<string, (string Value, DateTimeOffset? ExpiresUtc)> _items = new(StringComparer.Ordinal);
    private readonly Lock _gate = new();

    public ValueTask<string?> GetStringAsync(string key, CancellationToken ct = default)
    {
        lock (_gate)
        {
            if (!_items.TryGetValue(key, out var value))
            {
                return ValueTask.FromResult<string?>(null);
            }

            if (value.ExpiresUtc is not null && value.ExpiresUtc.Value <= DateTimeOffset.UtcNow)
            {
                _items.Remove(key);
                return ValueTask.FromResult<string?>(null);
            }

            return ValueTask.FromResult<string?>(value.Value);
        }
    }

    public ValueTask SetStringAsync(string key, string value, TimeSpan? ttl = null, CancellationToken ct = default)
    {
        lock (_gate)
        {
            DateTimeOffset? expires = ttl is null || ttl.Value <= TimeSpan.Zero ? null : DateTimeOffset.UtcNow + ttl.Value;
            _items[key] = (value, expires);
        }

        return ValueTask.CompletedTask;
    }

    public async IAsyncEnumerable<KeyValuePair<string, string>> EnumerateByPrefixAsync(string prefix, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        KeyValuePair<string, string>[] items;
        lock (_gate)
        {
            var now = DateTimeOffset.UtcNow;
            foreach (var expiredKey in _items.Where(x => x.Value.ExpiresUtc is not null && x.Value.ExpiresUtc.Value <= now).Select(static x => x.Key).ToArray())
            {
                _items.Remove(expiredKey);
            }

            items = _items
                .Where(x => x.Key.StartsWith(prefix, StringComparison.Ordinal))
                .Select(static x => new KeyValuePair<string, string>(x.Key, x.Value.Value))
                .ToArray();
        }

        foreach (var item in items)
        {
            ct.ThrowIfCancellationRequested();
            yield return item;
            await Task.Yield();
        }
    }
}

