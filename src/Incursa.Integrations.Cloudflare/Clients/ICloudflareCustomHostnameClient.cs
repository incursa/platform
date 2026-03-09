using Incursa.Integrations.Cloudflare.Clients.Models;

namespace Incursa.Integrations.Cloudflare.Clients;

public interface ICloudflareCustomHostnameClient
{
    Task<CloudflareCustomHostname?> GetByHostnameAsync(string hostname, CancellationToken cancellationToken = default);

    Task<CloudflareCustomHostname?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    Task<CloudflareCustomHostname> CreateAsync(CloudflareCreateCustomHostnameRequest request, CancellationToken cancellationToken = default);

    Task<CloudflareCustomHostname> PatchAsync(string id, CloudflarePatchCustomHostnameRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(string id, CancellationToken cancellationToken = default);
}
