using Incursa.Integrations.Cloudflare.Clients.Models;

namespace Incursa.Integrations.Cloudflare.Clients;

public interface ICloudflareLoadBalancerClient
{
    Task<IReadOnlyList<CloudflareLoadBalancer>> ListAsync(CancellationToken cancellationToken = default);

    Task<CloudflareLoadBalancer> GetAsync(string id, CancellationToken cancellationToken = default);

    Task<CloudflareLoadBalancer> CreateAsync(CloudflareCreateLoadBalancerRequest request, CancellationToken cancellationToken = default);

    Task<CloudflareLoadBalancer> UpdateAsync(string id, CloudflareUpdateLoadBalancerRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(string id, CancellationToken cancellationToken = default);

    Task<CloudflareLoadBalancerPreviewResult> PreviewAsync(string id, CancellationToken cancellationToken = default);
}
