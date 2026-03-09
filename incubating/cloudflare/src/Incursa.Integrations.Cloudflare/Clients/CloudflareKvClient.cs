using System.Net;
using Incursa.Integrations.Cloudflare.Internal;
using Incursa.Integrations.Cloudflare.Options;
using Microsoft.Extensions.Options;

namespace Incursa.Integrations.Cloudflare.Clients;

public sealed class CloudflareKvClient : ICloudflareKvClient
{
    private readonly CloudflareApiTransport transport;
    private readonly CloudflareApiOptions apiOptions;
    private readonly CloudflareKvOptions kvOptions;

    public CloudflareKvClient(
        CloudflareApiTransport transport,
        IOptions<CloudflareApiOptions> apiOptions,
        IOptions<CloudflareKvOptions> kvOptions)
    {
        this.transport = transport ?? throw new ArgumentNullException(nameof(transport));
        this.apiOptions = apiOptions?.Value ?? throw new ArgumentNullException(nameof(apiOptions));
        this.kvOptions = kvOptions?.Value ?? throw new ArgumentNullException(nameof(kvOptions));
    }

    public async Task<string?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        var path = BuildValuesPath(key);
        var response = await transport.SendForRawAsync(HttpMethod.Get, path, body: null, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        if ((int)response.StatusCode < 200 || (int)response.StatusCode >= 300)
        {
            throw new InvalidOperationException($"Cloudflare KV GET failed status={(int)response.StatusCode} cf-ray={response.CfRay ?? "<none>"} body={response.Body}");
        }

        return response.Body;
    }

    public async Task PutAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        _ = await transport.SendForResultAsync<JsonElement>(HttpMethod.Put, BuildValuesPath(key), value, cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        _ = await transport.SendForResultAsync<JsonElement>(HttpMethod.Delete, BuildValuesPath(key), body: null, cancellationToken).ConfigureAwait(false);
    }

    public async IAsyncEnumerable<string> ListKeysAsync(string prefix, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var listOperationTimeoutSeconds = kvOptions.ListOperationTimeoutSeconds;
        using var listTimeoutCts = listOperationTimeoutSeconds > 0
            ? new CancellationTokenSource(TimeSpan.FromSeconds(listOperationTimeoutSeconds))
            : null;
        using var linkedCts = listTimeoutCts is null
            ? null
            : CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, listTimeoutCts.Token);
        var effectiveCancellationToken = linkedCts?.Token ?? cancellationToken;

        string? cursor = null;
        HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
        var page = 0;

        async Task<CloudflareApiTransport.CloudflareApiEnvelope<KvKeyItem[]>> SendListPageAsync(string query)
        {
            try
            {
                return await transport.SendForEnvelopeAsync<KvKeyItem[]>(HttpMethod.Get, query, body: null, effectiveCancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException ex) when (
                listTimeoutCts is not null &&
                listTimeoutCts.IsCancellationRequested &&
                !cancellationToken.IsCancellationRequested)
            {
                throw new TimeoutException(
                    $"Cloudflare KV key listing timed out after {listOperationTimeoutSeconds} second(s).",
                    ex);
            }
        }

        do
        {
            effectiveCancellationToken.ThrowIfCancellationRequested();
            if (page++ > 5000)
            {
                throw new InvalidOperationException("Cloudflare KV key listing exceeded maximum page count.");
            }

            var query = $"accounts/{Required(kvOptions.AccountId, apiOptions.AccountId, nameof(CloudflareKvOptions.AccountId))}/storage/kv/namespaces/{Required(kvOptions.NamespaceId, null, nameof(CloudflareKvOptions.NamespaceId))}/keys?prefix={Uri.EscapeDataString(prefix ?? string.Empty)}&limit=1000";
            if (!string.IsNullOrWhiteSpace(cursor))
            {
                query += $"&cursor={Uri.EscapeDataString(cursor)}";
            }

            var envelope = await SendListPageAsync(query).ConfigureAwait(false);
            foreach (var item in envelope.Result ?? Array.Empty<KvKeyItem>())
            {
                effectiveCancellationToken.ThrowIfCancellationRequested();
                if (!string.IsNullOrWhiteSpace(item.Name))
                {
                    yield return item.Name;
                }
            }

            var resultInfo = envelope.ResultInfo.HasValue
                ? envelope.ResultInfo.Value.Deserialize<KvResultInfo>()
                : null;

            if (string.IsNullOrWhiteSpace(resultInfo?.Cursor))
            {
                cursor = null;
                continue;
            }

            if (!seen.Add(resultInfo.Cursor))
            {
                throw new InvalidOperationException("Cloudflare KV key listing returned a repeated cursor.");
            }

            cursor = resultInfo.Cursor;
        }
        while (!string.IsNullOrWhiteSpace(cursor));
    }

    private string BuildValuesPath(string key)
        => $"accounts/{Required(kvOptions.AccountId, apiOptions.AccountId, nameof(CloudflareKvOptions.AccountId))}/storage/kv/namespaces/{Required(kvOptions.NamespaceId, null, nameof(CloudflareKvOptions.NamespaceId))}/values/{EscapePath(key)}";

    private static string EscapePath(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Key is required.", nameof(key));
        }

        return Uri.EscapeDataString(key.Trim());
    }

    private static string Required(string? preferred, string? fallback, string name)
    {
        var candidate = string.IsNullOrWhiteSpace(preferred) ? fallback : preferred;
        if (string.IsNullOrWhiteSpace(candidate))
        {
            throw new InvalidOperationException($"Cloudflare option '{name}' is required.");
        }

        return candidate.Trim();
    }

    private sealed record KvKeyItem([property: JsonPropertyName("name")] string Name);

    private sealed record KvResultInfo([property: JsonPropertyName("cursor")] string? Cursor);
}
