using Incursa.Integrations.Cloudflare.Clients.Models;
using Incursa.Integrations.Cloudflare.Internal;
using Incursa.Integrations.Cloudflare.Options;
using Microsoft.Extensions.Options;

namespace Incursa.Integrations.Cloudflare.Clients;

public sealed class CloudflareLoadBalancerClient : ICloudflareLoadBalancerClient
{
    private readonly CloudflareApiTransport transport;
    private readonly CloudflareApiOptions apiOptions;
    private readonly CloudflareLoadBalancerOptions loadBalancerOptions;

    public CloudflareLoadBalancerClient(
        CloudflareApiTransport transport,
        IOptions<CloudflareApiOptions> apiOptions,
        IOptions<CloudflareLoadBalancerOptions> loadBalancerOptions)
    {
        this.transport = transport ?? throw new ArgumentNullException(nameof(transport));
        this.apiOptions = apiOptions?.Value ?? throw new ArgumentNullException(nameof(apiOptions));
        this.loadBalancerOptions = loadBalancerOptions?.Value ?? throw new ArgumentNullException(nameof(loadBalancerOptions));
    }

    public async Task<IReadOnlyList<CloudflareLoadBalancer>> ListAsync(CancellationToken cancellationToken = default)
    {
        var result = await transport.SendForResultAsync<CloudflareListResult<CloudflareLoadBalancer>>(HttpMethod.Get, $"zones/{ZoneId()}/load_balancers", body: null, cancellationToken).ConfigureAwait(false);
        return result.Items;
    }

    public Task<CloudflareLoadBalancer> GetAsync(string id, CancellationToken cancellationToken = default)
        => transport.SendForResultAsync<CloudflareLoadBalancer>(HttpMethod.Get, $"zones/{ZoneId()}/load_balancers/{Escape(id)}", body: null, cancellationToken);

    public Task<CloudflareLoadBalancer> CreateAsync(CloudflareCreateLoadBalancerRequest request, CancellationToken cancellationToken = default)
        => transport.SendForResultAsync<CloudflareLoadBalancer>(HttpMethod.Post, $"zones/{ZoneId()}/load_balancers", request, cancellationToken);

    public Task<CloudflareLoadBalancer> UpdateAsync(string id, CloudflareUpdateLoadBalancerRequest request, CancellationToken cancellationToken = default)
        => transport.SendForResultAsync<CloudflareLoadBalancer>(HttpMethod.Put, $"zones/{ZoneId()}/load_balancers/{Escape(id)}", request, cancellationToken);

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        _ = await transport.SendForResultAsync<JsonElement>(HttpMethod.Delete, $"zones/{ZoneId()}/load_balancers/{Escape(id)}", body: null, cancellationToken).ConfigureAwait(false);
    }

    public Task<CloudflareLoadBalancerPreviewResult> PreviewAsync(string id, CancellationToken cancellationToken = default)
        => transport.SendForResultAsync<CloudflareLoadBalancerPreviewResult>(HttpMethod.Get, $"zones/{ZoneId()}/load_balancers/{Escape(id)}/preview", body: null, cancellationToken);

    private string ZoneId()
        => Required(loadBalancerOptions.ZoneId, apiOptions.ZoneId, "ZoneId");

    private static string Required(string? preferred, string? fallback, string name)
    {
        var candidate = string.IsNullOrWhiteSpace(preferred) ? fallback : preferred;
        if (string.IsNullOrWhiteSpace(candidate))
        {
            throw new InvalidOperationException($"Cloudflare option '{name}' is required.");
        }

        return candidate.Trim();
    }

    private static string Escape(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Id is required.", nameof(id));
        }

        return Uri.EscapeDataString(id.Trim());
    }
}
