namespace Incursa.Platform.CustomDomains.Internal;

using Incursa.Platform.Storage;

internal sealed class CustomDomainStorageContext
{
    public CustomDomainStorageContext(
        IRecordStore<CustomDomain> domains,
        ILookupStore<DomainByHostnameProjection> domainsByHostname,
        ILookupStore<DomainByExternalLinkProjection> domainsByExternalLink)
    {
        Domains = domains ?? throw new ArgumentNullException(nameof(domains));
        DomainsByHostname = domainsByHostname ?? throw new ArgumentNullException(nameof(domainsByHostname));
        DomainsByExternalLink = domainsByExternalLink ?? throw new ArgumentNullException(nameof(domainsByExternalLink));
    }

    public IRecordStore<CustomDomain> Domains { get; }

    public ILookupStore<DomainByHostnameProjection> DomainsByHostname { get; }

    public ILookupStore<DomainByExternalLinkProjection> DomainsByExternalLink { get; }
}
