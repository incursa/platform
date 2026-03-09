namespace Incursa.Platform.CustomDomains;

using Incursa.Platform.CustomDomains.Internal;

internal sealed class StorageBackedCustomDomainQueryService : ICustomDomainQueryService
{
    private readonly CustomDomainStorageContext storage;

    public StorageBackedCustomDomainQueryService(CustomDomainStorageContext storage)
    {
        this.storage = storage ?? throw new ArgumentNullException(nameof(storage));
    }

    public async Task<CustomDomain?> GetDomainAsync(CustomDomainId domainId, CancellationToken cancellationToken = default)
    {
        var item = await storage.Domains.GetAsync(CustomDomainStorageKeys.Domain(domainId), cancellationToken).ConfigureAwait(false);
        return item?.Value;
    }

    public async Task<CustomDomain?> GetDomainByHostnameAsync(string hostname, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hostname);

        var item = await storage.DomainsByHostname.GetAsync(
            CustomDomainStorageKeys.DomainByHostname(hostname),
            cancellationToken).ConfigureAwait(false);
        return item?.Value.Domain;
    }

    public async Task<CustomDomain?> GetDomainByExternalLinkAsync(
        string provider,
        string externalId,
        string? resourceType = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);
        ArgumentException.ThrowIfNullOrWhiteSpace(externalId);

        var item = await storage.DomainsByExternalLink.GetAsync(
            CustomDomainStorageKeys.DomainByExternalLink(provider, externalId, resourceType),
            cancellationToken).ConfigureAwait(false);
        return item?.Value.Domain;
    }
}
