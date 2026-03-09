using Incursa.Integrations.Cloudflare.Clients.Models;

namespace Incursa.Integrations.Cloudflare.Clients;

public interface ICloudflareLoadBalancerMonitorClient
{
    Task<IReadOnlyList<CloudflareLoadBalancerMonitor>> ListAsync(CancellationToken cancellationToken = default);

    Task<CloudflareLoadBalancerMonitor> GetAsync(string id, CancellationToken cancellationToken = default);

    Task<CloudflareLoadBalancerMonitor> CreateAsync(CloudflareCreateLoadBalancerMonitorRequest request, CancellationToken cancellationToken = default);

    Task<CloudflareLoadBalancerMonitor> UpdateAsync(string id, CloudflareUpdateLoadBalancerMonitorRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(string id, CancellationToken cancellationToken = default);
}
