using System.Collections.Concurrent;

namespace Incursa.Integrations.Cloudflare.Storage;

/// <summary>
/// Provides a deterministic in-memory implementation of <see cref="ICloudflareKvStore"/> for tests.
/// </summary>
public sealed class InMemoryCloudflareKvStore : ICloudflareKvStore
{
    private readonly ConcurrentDictionary<string, string> values = new(StringComparer.Ordinal);

    /// <inheritdoc/>
    public ValueTask<string?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return new(values.TryGetValue(key, out var value) ? value : null);
    }

    /// <inheritdoc/>
    public ValueTask PutAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        values[key] = value;
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc/>
    public ValueTask DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _ = values.TryRemove(key, out _);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<string> ListKeysAsync(
        string prefix,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var effectivePrefix = prefix ?? string.Empty;
        var keys = values.Keys
            .Where(static key => !string.IsNullOrWhiteSpace(key))
            .Where(key => key.StartsWith(effectivePrefix, StringComparison.Ordinal))
            .OrderBy(static key => key, StringComparer.Ordinal)
            .ToArray();

        foreach (var key in keys)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return key;
            await Task.CompletedTask.ConfigureAwait(false);
        }
    }
}
