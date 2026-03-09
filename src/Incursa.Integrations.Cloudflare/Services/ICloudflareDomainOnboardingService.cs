using Incursa.Integrations.Cloudflare.Clients.Models;

namespace Incursa.Integrations.Cloudflare.Services;

public interface ICloudflareDomainOnboardingService
{
    Task<CloudflareDomainOnboardingResult> CreateOrFetchCustomHostnameAsync(string hostname, CancellationToken cancellationToken = default);
}

public interface ICloudflareDomainSyncService
{
    Task<CloudflareDomainOnboardingResult?> SyncByIdAsync(string customHostnameId, CancellationToken cancellationToken = default);

    Task<CloudflareDomainOnboardingResult?> SyncByHostnameAsync(string hostname, CancellationToken cancellationToken = default);
}

public sealed record CloudflareDomainOnboardingResult(
    string? Id,
    string? Hostname,
    string? Status,
    string? SslStatus,
    string? SslMethod,
    string? SslValidationError,
    string? OwnershipVerificationName,
    string? OwnershipVerificationType,
    string? OwnershipVerificationValue,
    DateTimeOffset SyncedUtc)
{
    public static CloudflareDomainOnboardingResult FromCloudflare(CloudflareCustomHostname source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var firstSslError = source.Ssl?.ValidationErrors is { Count: > 0 } errors ? errors[0].Message : null;
        return new CloudflareDomainOnboardingResult(
            source.Id,
            source.Hostname,
            source.Status,
            source.Ssl?.Status,
            source.Ssl?.Method,
            firstSslError,
            source.OwnershipVerification?.Name,
            source.OwnershipVerification?.Type,
            source.OwnershipVerification?.Value,
            DateTimeOffset.UtcNow);
    }
}
