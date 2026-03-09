namespace Incursa.Platform.CustomDomains;

using Incursa.Platform.CustomDomains.Internal;
using Incursa.Platform.Storage;

internal sealed class StorageBackedCustomDomainAdministrationService : ICustomDomainAdministrationService
{
    private readonly CustomDomainStorageContext storage;

    public StorageBackedCustomDomainAdministrationService(CustomDomainStorageContext storage)
    {
        this.storage = storage ?? throw new ArgumentNullException(nameof(storage));
    }

    public async Task<CustomDomain> UpsertDomainAsync(CustomDomain domain, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domain);

        var normalized = CustomDomainNormalizer.Normalize(domain);
        var previous = await storage.Domains.GetAsync(CustomDomainStorageKeys.Domain(normalized.Id), cancellationToken).ConfigureAwait(false);

        await storage.Domains.WriteAsync(
            CustomDomainStorageKeys.Domain(normalized.Id),
            normalized,
            StorageWriteMode.Upsert,
            StorageWriteCondition.Unconditional(),
            cancellationToken).ConfigureAwait(false);

        if (previous is not null)
        {
            _ = await storage.DomainsByHostname.DeleteAsync(
                CustomDomainStorageKeys.DomainByHostname(previous.Value.Hostname),
                StorageWriteCondition.Unconditional(),
                cancellationToken).ConfigureAwait(false);

            foreach (var link in previous.Value.ExternalLinks)
            {
                _ = await storage.DomainsByExternalLink.DeleteAsync(
                    CustomDomainStorageKeys.DomainByExternalLink(link.Provider, link.ExternalId, link.ResourceType),
                    StorageWriteCondition.Unconditional(),
                    cancellationToken).ConfigureAwait(false);
            }
        }

        await storage.DomainsByHostname.UpsertAsync(
            CustomDomainStorageKeys.DomainByHostname(normalized.Hostname),
            new DomainByHostnameProjection(normalized),
            StorageWriteCondition.Unconditional(),
            cancellationToken).ConfigureAwait(false);

        foreach (var link in normalized.ExternalLinks)
        {
            await storage.DomainsByExternalLink.UpsertAsync(
                CustomDomainStorageKeys.DomainByExternalLink(link.Provider, link.ExternalId, link.ResourceType),
                new DomainByExternalLinkProjection(normalized),
                StorageWriteCondition.Unconditional(),
                cancellationToken).ConfigureAwait(false);
        }

        return normalized;
    }

    public async Task<bool> DeleteDomainAsync(CustomDomainId domainId, CancellationToken cancellationToken = default)
    {
        var existing = await storage.Domains.GetAsync(CustomDomainStorageKeys.Domain(domainId), cancellationToken).ConfigureAwait(false);
        if (existing is null)
        {
            return false;
        }

        _ = await storage.Domains.DeleteAsync(
            CustomDomainStorageKeys.Domain(domainId),
            StorageWriteCondition.Unconditional(),
            cancellationToken).ConfigureAwait(false);

        _ = await storage.DomainsByHostname.DeleteAsync(
            CustomDomainStorageKeys.DomainByHostname(existing.Value.Hostname),
            StorageWriteCondition.Unconditional(),
            cancellationToken).ConfigureAwait(false);

        foreach (var link in existing.Value.ExternalLinks)
        {
            _ = await storage.DomainsByExternalLink.DeleteAsync(
                CustomDomainStorageKeys.DomainByExternalLink(link.Provider, link.ExternalId, link.ResourceType),
                StorageWriteCondition.Unconditional(),
                cancellationToken).ConfigureAwait(false);
        }

        return true;
    }
}
