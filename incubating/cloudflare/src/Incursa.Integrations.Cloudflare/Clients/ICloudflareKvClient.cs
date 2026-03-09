namespace Incursa.Integrations.Cloudflare.Clients;

public interface ICloudflareKvClient
{
    Task<string?> GetAsync(string key, CancellationToken cancellationToken = default);

    Task PutAsync(string key, string value, CancellationToken cancellationToken = default);

    Task DeleteAsync(string key, CancellationToken cancellationToken = default);

    IAsyncEnumerable<string> ListKeysAsync(string prefix, CancellationToken cancellationToken = default);
}
