namespace Incursa.Integrations.Cloudflare.Storage;

public interface ICloudflareKvStore
{
    ValueTask<string?> GetAsync(string key, CancellationToken cancellationToken = default);

    ValueTask PutAsync(string key, string value, CancellationToken cancellationToken = default);

    ValueTask DeleteAsync(string key, CancellationToken cancellationToken = default);

    IAsyncEnumerable<string> ListKeysAsync(string prefix, CancellationToken cancellationToken = default);
}
