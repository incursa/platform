using Incursa.Integrations.Cloudflare.Clients.Models;
using Incursa.Integrations.Cloudflare.Internal;
using Incursa.Integrations.Cloudflare.Options;
using Microsoft.Extensions.Options;

namespace Incursa.Integrations.Cloudflare.Clients;

public sealed class CloudflareLoadBalancerMonitorClient : ICloudflareLoadBalancerMonitorClient
{
    private readonly CloudflareApiTransport transport;
    private readonly CloudflareApiOptions apiOptions;
    private readonly CloudflareLoadBalancerOptions loadBalancerOptions;

    public CloudflareLoadBalancerMonitorClient(
        CloudflareApiTransport transport,
        IOptions<CloudflareApiOptions> apiOptions,
        IOptions<CloudflareLoadBalancerOptions> loadBalancerOptions)
    {
        this.transport = transport ?? throw new ArgumentNullException(nameof(transport));
        this.apiOptions = apiOptions?.Value ?? throw new ArgumentNullException(nameof(apiOptions));
        this.loadBalancerOptions = loadBalancerOptions?.Value ?? throw new ArgumentNullException(nameof(loadBalancerOptions));
    }

    public async Task<IReadOnlyList<CloudflareLoadBalancerMonitor>> ListAsync(CancellationToken cancellationToken = default)
    {
        var result = await transport.SendForResultAsync<CloudflareListResult<CloudflareLoadBalancerMonitor>>(HttpMethod.Get, $"accounts/{AccountId()}/load_balancers/monitors", body: null, cancellationToken).ConfigureAwait(false);
        return result.Items;
    }

    public Task<CloudflareLoadBalancerMonitor> GetAsync(string id, CancellationToken cancellationToken = default)
        => transport.SendForResultAsync<CloudflareLoadBalancerMonitor>(HttpMethod.Get, $"accounts/{AccountId()}/load_balancers/monitors/{Escape(id)}", body: null, cancellationToken);

    public Task<CloudflareLoadBalancerMonitor> CreateAsync(CloudflareCreateLoadBalancerMonitorRequest request, CancellationToken cancellationToken = default)
        => transport.SendForResultAsync<CloudflareLoadBalancerMonitor>(HttpMethod.Post, $"accounts/{AccountId()}/load_balancers/monitors", request, cancellationToken);

    public Task<CloudflareLoadBalancerMonitor> UpdateAsync(string id, CloudflareUpdateLoadBalancerMonitorRequest request, CancellationToken cancellationToken = default)
        => transport.SendForResultAsync<CloudflareLoadBalancerMonitor>(HttpMethod.Put, $"accounts/{AccountId()}/load_balancers/monitors/{Escape(id)}", request, cancellationToken);

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        _ = await transport.SendForResultAsync<JsonElement>(HttpMethod.Delete, $"accounts/{AccountId()}/load_balancers/monitors/{Escape(id)}", body: null, cancellationToken).ConfigureAwait(false);
    }

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
