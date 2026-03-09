using Incursa.Integrations.Cloudflare.Clients.Models;
using Incursa.Integrations.Cloudflare.Internal;
using Incursa.Integrations.Cloudflare.Options;
using Microsoft.Extensions.Options;

namespace Incursa.Integrations.Cloudflare.Clients;

public sealed class CloudflareLoadBalancerPoolClient : ICloudflareLoadBalancerPoolClient
{
    private readonly CloudflareApiTransport transport;
    private readonly CloudflareApiOptions apiOptions;
    private readonly CloudflareLoadBalancerOptions loadBalancerOptions;

    public CloudflareLoadBalancerPoolClient(
        CloudflareApiTransport transport,
        IOptions<CloudflareApiOptions> apiOptions,
        IOptions<CloudflareLoadBalancerOptions> loadBalancerOptions)
    {
        this.transport = transport ?? throw new ArgumentNullException(nameof(transport));
        this.apiOptions = apiOptions?.Value ?? throw new ArgumentNullException(nameof(apiOptions));
        this.loadBalancerOptions = loadBalancerOptions?.Value ?? throw new ArgumentNullException(nameof(loadBalancerOptions));
    }

    public async Task<IReadOnlyList<CloudflareLoadBalancerPool>> ListAsync(CancellationToken cancellationToken = default)
    {
        var result = await transport.SendForResultAsync<CloudflareListResult<CloudflareLoadBalancerPool>>(HttpMethod.Get, $"accounts/{AccountId()}/load_balancers/pools", body: null, cancellationToken).ConfigureAwait(false);
        return result.Items;
    }

    public Task<CloudflareLoadBalancerPool> GetAsync(string id, CancellationToken cancellationToken = default)
        => transport.SendForResultAsync<CloudflareLoadBalancerPool>(HttpMethod.Get, $"accounts/{AccountId()}/load_balancers/pools/{Escape(id)}", body: null, cancellationToken);

    public Task<CloudflareLoadBalancerPool> CreateAsync(CloudflareCreateLoadBalancerPoolRequest request, CancellationToken cancellationToken = default)
        => transport.SendForResultAsync<CloudflareLoadBalancerPool>(HttpMethod.Post, $"accounts/{AccountId()}/load_balancers/pools", request, cancellationToken);

    public Task<CloudflareLoadBalancerPool> UpdateAsync(string id, CloudflareUpdateLoadBalancerPoolRequest request, CancellationToken cancellationToken = default)
        => transport.SendForResultAsync<CloudflareLoadBalancerPool>(HttpMethod.Put, $"accounts/{AccountId()}/load_balancers/pools/{Escape(id)}", request, cancellationToken);

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        _ = await transport.SendForResultAsync<JsonElement>(HttpMethod.Delete, $"accounts/{AccountId()}/load_balancers/pools/{Escape(id)}", body: null, cancellationToken).ConfigureAwait(false);
    }

    public Task<CloudflarePoolHealthResult> HealthAsync(string id, CancellationToken cancellationToken = default)
        => transport.SendForResultAsync<CloudflarePoolHealthResult>(HttpMethod.Get, $"accounts/{AccountId()}/load_balancers/pools/{Escape(id)}/health", body: null, cancellationToken);

    private string AccountId()
        => Required(loadBalancerOptions.AccountId, apiOptions.AccountId, "AccountId");

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
