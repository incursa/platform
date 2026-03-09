using System.Security.Cryptography;
using Incursa.Integrations.Cloudflare.Clients;
using Microsoft.Extensions.Logging;

namespace Incursa.Integrations.Cloudflare.Storage;

public sealed class CloudflareKvStore : ICloudflareKvStore
{
    private static readonly Action<ILogger, string, Exception?> LogNullValueMessage =
        LoggerMessage.Define<string>(
            LogLevel.Debug,
            new EventId(1, nameof(LogNullValueMessage)),
            "Cloudflare KV GET key={Key} returned null.");

    private readonly ICloudflareKvClient client;
    private readonly ILogger<CloudflareKvStore> logger;

    public CloudflareKvStore(ICloudflareKvClient client, ILogger<CloudflareKvStore> logger)
    {
        this.client = client ?? throw new ArgumentNullException(nameof(client));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async ValueTask<string?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        var value = await client.GetAsync(key, cancellationToken).ConfigureAwait(false);
        if (value is null && logger.IsEnabled(LogLevel.Debug))
        {
            LogNullValueMessage(logger, RedactKey(key), null);
        }

        return value;
    }

    public ValueTask PutAsync(string key, string value, CancellationToken cancellationToken = default)
        => new(client.PutAsync(key, value, cancellationToken));

    public ValueTask DeleteAsync(string key, CancellationToken cancellationToken = default)
        => new(client.DeleteAsync(key, cancellationToken));

    public IAsyncEnumerable<string> ListKeysAsync(string prefix, CancellationToken cancellationToken = default)
        => client.ListKeysAsync(prefix, cancellationToken);

    private static string RedactKey(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "<empty>";
        }

        var trimmed = value.Trim();
        var prefix = trimmed.Length <= 12 ? trimmed : trimmed[..12];
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(trimmed)))[..8];
        return $"{prefix}...#{hash}";
    }
}
