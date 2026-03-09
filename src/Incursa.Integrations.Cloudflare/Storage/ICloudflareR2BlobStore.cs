namespace Incursa.Integrations.Cloudflare.Storage;

public interface ICloudflareR2BlobStore
{
    ValueTask PutAsync(string key, Stream content, string? contentType = null, CancellationToken cancellationToken = default);

    ValueTask<Stream?> GetAsync(string key, CancellationToken cancellationToken = default);

    ValueTask<Stream?> GetRangeAsync(string key, long startInclusive, long endInclusive, CancellationToken cancellationToken = default);

    ValueTask<CloudflareBlobObjectInfo?> HeadAsync(string key, CancellationToken cancellationToken = default);

    IAsyncEnumerable<CloudflareBlobObjectInfo> ListAsync(string prefix, CancellationToken cancellationToken = default);

    ValueTask DeleteAsync(string key, CancellationToken cancellationToken = default);
}
