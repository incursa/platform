using System.Collections.Concurrent;

namespace Incursa.Integrations.Cloudflare.Storage;

/// <summary>
/// Provides a deterministic in-memory implementation of <see cref="ICloudflareR2BlobStore"/> for tests.
/// </summary>
public sealed class InMemoryCloudflareR2BlobStore : ICloudflareR2BlobStore
{
    private readonly ConcurrentDictionary<string, StoredBlob> blobs = new(StringComparer.Ordinal);

    /// <inheritdoc/>
    public async ValueTask PutAsync(string key, Stream content, string? contentType = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        cancellationToken.ThrowIfCancellationRequested();

        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);

        var blob = new StoredBlob(buffer.ToArray(), contentType, DateTimeOffset.UtcNow);
        blobs[key] = blob;
    }

    /// <inheritdoc/>
    public ValueTask<Stream?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!blobs.TryGetValue(key, out var blob))
        {
            return new((Stream?)null);
        }

        return new((Stream)new MemoryStream(blob.Content, writable: false));
    }

    /// <inheritdoc/>
    public ValueTask<Stream?> GetRangeAsync(string key, long startInclusive, long endInclusive, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (startInclusive < 0 || endInclusive < startInclusive)
        {
            return new((Stream?)null);
        }

        if (!blobs.TryGetValue(key, out var blob))
        {
            return new((Stream?)null);
        }

        if (startInclusive >= blob.Content.LongLength)
        {
            return new((Stream?)null);
        }

        var clampedEnd = Math.Min(endInclusive, blob.Content.LongLength - 1);
        var length = checked((int)((clampedEnd - startInclusive) + 1));
        var output = new byte[length];

        Buffer.BlockCopy(blob.Content, checked((int)startInclusive), output, 0, length);
        return new((Stream)new MemoryStream(output, writable: false));
    }

    /// <inheritdoc/>
    public ValueTask<CloudflareBlobObjectInfo?> HeadAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!blobs.TryGetValue(key, out var blob))
        {
            return new((CloudflareBlobObjectInfo?)null);
        }

        return new(new CloudflareBlobObjectInfo(
            key,
            blob.Content.LongLength,
            blob.ContentType,
            blob.LastModifiedUtc));
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<CloudflareBlobObjectInfo> ListAsync(
        string prefix,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var effectivePrefix = prefix ?? string.Empty;
        var keys = blobs.Keys
            .Where(static key => !string.IsNullOrWhiteSpace(key))
            .Where(key => key.StartsWith(effectivePrefix, StringComparison.Ordinal))
            .OrderBy(static key => key, StringComparer.Ordinal)
            .ToArray();

        foreach (var key in keys)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (blobs.TryGetValue(key, out var blob))
            {
                yield return new CloudflareBlobObjectInfo(
                    key,
                    blob.Content.LongLength,
                    blob.ContentType,
                    blob.LastModifiedUtc);
            }

            await Task.CompletedTask.ConfigureAwait(false);
        }
    }

    /// <inheritdoc/>
    public ValueTask DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _ = blobs.TryRemove(key, out _);
        return ValueTask.CompletedTask;
    }

    private sealed record StoredBlob(byte[] Content, string? ContentType, DateTimeOffset LastModifiedUtc);
}
