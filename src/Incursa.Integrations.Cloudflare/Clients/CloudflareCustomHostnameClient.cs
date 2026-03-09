using Incursa.Integrations.Cloudflare.Clients.Models;
using Incursa.Integrations.Cloudflare.Internal;
using Incursa.Integrations.Cloudflare.Options;
using Microsoft.Extensions.Options;

namespace Incursa.Integrations.Cloudflare.Clients;

public sealed class CloudflareCustomHostnameClient : ICloudflareCustomHostnameClient
{
    private readonly CloudflareApiTransport transport;
    private readonly CloudflareCustomHostnameOptions customHostnameOptions;
    private readonly CloudflareApiOptions apiOptions;

    public CloudflareCustomHostnameClient(
        CloudflareApiTransport transport,
        IOptions<CloudflareCustomHostnameOptions> customHostnameOptions,
        IOptions<CloudflareApiOptions> apiOptions)
    {
        this.transport = transport ?? throw new ArgumentNullException(nameof(transport));
        this.customHostnameOptions = customHostnameOptions?.Value ?? throw new ArgumentNullException(nameof(customHostnameOptions));
        this.apiOptions = apiOptions?.Value ?? throw new ArgumentNullException(nameof(apiOptions));
    }

    public async Task<CloudflareCustomHostname?> GetByHostnameAsync(string hostname, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(hostname))
        {
            throw new ArgumentException("Hostname is required.", nameof(hostname));
        }

        var query = Uri.EscapeDataString(hostname.Trim());
        var path = $"zones/{GetZoneId()}/custom_hostnames?hostname={query}";
        var result = await transport.SendForResultAsync<CloudflareCustomHostnameListResult>(HttpMethod.Get, path, body: null, cancellationToken).ConfigureAwait(false);
        return result.Items.Count > 0 ? result.Items[0] : null;
    }

    public async Task<CloudflareCustomHostname?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        var path = $"zones/{GetZoneId()}/custom_hostnames/{Uri.EscapeDataString(id.Trim())}";
        return await transport.SendForResultAsync<CloudflareCustomHostname>(HttpMethod.Get, path, body: null, cancellationToken).ConfigureAwait(false);
    }

    public Task<CloudflareCustomHostname> CreateAsync(CloudflareCreateCustomHostnameRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var path = $"zones/{GetZoneId()}/custom_hostnames";
        return transport.SendForResultAsync<CloudflareCustomHostname>(HttpMethod.Post, path, request, cancellationToken);
    }

    public Task<CloudflareCustomHostname> PatchAsync(string id, CloudflarePatchCustomHostnameRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Id is required.", nameof(id));
        }

        ArgumentNullException.ThrowIfNull(request);
        var path = $"zones/{GetZoneId()}/custom_hostnames/{Uri.EscapeDataString(id.Trim())}";
        return transport.SendForResultAsync<CloudflareCustomHostname>(HttpMethod.Patch, path, request, cancellationToken);
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Id is required.", nameof(id));
        }

        var path = $"zones/{GetZoneId()}/custom_hostnames/{Uri.EscapeDataString(id.Trim())}";
        _ = await transport.SendForResultAsync<JsonElement>(HttpMethod.Delete, path, body: null, cancellationToken).ConfigureAwait(false);
    }

    private string GetZoneId()
    {
        var value = string.IsNullOrWhiteSpace(customHostnameOptions.ZoneId)
            ? apiOptions.ZoneId
            : customHostnameOptions.ZoneId;
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException("Cloudflare ZoneId is required for custom hostnames.");
        }

        return value.Trim();
    }
}
