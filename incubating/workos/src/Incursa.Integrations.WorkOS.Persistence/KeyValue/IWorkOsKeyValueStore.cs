namespace Incursa.Integrations.WorkOS.Persistence.KeyValue;

public interface IWorkOsKeyValueStore
{
    ValueTask<string?> GetStringAsync(string key, CancellationToken ct = default);

    ValueTask SetStringAsync(string key, string value, TimeSpan? ttl = null, CancellationToken ct = default);

    IAsyncEnumerable<KeyValuePair<string, string>> EnumerateByPrefixAsync(string prefix, CancellationToken ct = default);
}

