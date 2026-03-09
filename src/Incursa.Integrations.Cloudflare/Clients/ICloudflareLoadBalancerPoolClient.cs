using Incursa.Integrations.Cloudflare.Clients.Models;

namespace Incursa.Integrations.Cloudflare.Clients;

public interface ICloudflareLoadBalancerPoolClient
{
    Task<IReadOnlyList<CloudflareLoadBalancerPool>> ListAsync(CancellationToken cancellationToken = default);

    Task<CloudflareLoadBalancerPool> GetAsync(string id, CancellationToken cancellationToken = default);

    Task<CloudflareLoadBalancerPool> CreateAsync(CloudflareCreateLoadBalancerPoolRequest request, CancellationToken cancellationToken = default);

    Task<CloudflareLoadBalancerPool> UpdateAsync(string id, CloudflareUpdateLoadBalancerPoolRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(string id, CancellationToken cancellationToken = default);

    Task<CloudflarePoolHealthResult> HealthAsync(string id, CancellationToken cancellationToken = default);
}
