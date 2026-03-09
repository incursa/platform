using Incursa.Integrations.Cloudflare.Clients;
using Incursa.Integrations.Cloudflare.Clients.Models;

namespace Incursa.Integrations.Cloudflare.Services;

public sealed class CloudflareDomainOnboardingService : ICloudflareDomainOnboardingService, ICloudflareDomainSyncService
{
    private readonly ICloudflareCustomHostnameClient customHostnameClient;

    public CloudflareDomainOnboardingService(ICloudflareCustomHostnameClient customHostnameClient)
    {
        this.customHostnameClient = customHostnameClient ?? throw new ArgumentNullException(nameof(customHostnameClient));
    }

    public async Task<CloudflareDomainOnboardingResult> CreateOrFetchCustomHostnameAsync(string hostname, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(hostname))
        {
            throw new ArgumentException("Hostname is required.", nameof(hostname));
        }

        var normalizedHost = hostname.Trim().TrimEnd('.');
        var existing = await customHostnameClient.GetByHostnameAsync(normalizedHost, cancellationToken).ConfigureAwait(false);
        if (existing is not null)
        {
            return CloudflareDomainOnboardingResult.FromCloudflare(existing);
        }

        var created = await customHostnameClient.CreateAsync(
            new CloudflareCreateCustomHostnameRequest(normalizedHost, new CloudflareCustomHostnameSslRequest("http", "dv")),
            cancellationToken).ConfigureAwait(false);

        return CloudflareDomainOnboardingResult.FromCloudflare(created);
    }

    public async Task<CloudflareDomainOnboardingResult?> SyncByIdAsync(string customHostnameId, CancellationToken cancellationToken = default)
    {
        var result = await customHostnameClient.GetByIdAsync(customHostnameId, cancellationToken).ConfigureAwait(false);
        return result is null ? null : CloudflareDomainOnboardingResult.FromCloudflare(result);
    }

    public async Task<CloudflareDomainOnboardingResult?> SyncByHostnameAsync(string hostname, CancellationToken cancellationToken = default)
    {
        var result = await customHostnameClient.GetByHostnameAsync(hostname, cancellationToken).ConfigureAwait(false);
        return result is null ? null : CloudflareDomainOnboardingResult.FromCloudflare(result);
    }
}
