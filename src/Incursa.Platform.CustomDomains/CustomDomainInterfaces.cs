#pragma warning disable MA0048
namespace Incursa.Platform.CustomDomains;

public interface ICustomDomainAdministrationService
{
    Task<CustomDomain> UpsertDomainAsync(CustomDomain domain, CancellationToken cancellationToken = default);

    Task<bool> DeleteDomainAsync(CustomDomainId domainId, CancellationToken cancellationToken = default);
}

public interface ICustomDomainQueryService
{
    Task<CustomDomain?> GetDomainAsync(CustomDomainId domainId, CancellationToken cancellationToken = default);

    Task<CustomDomain?> GetDomainByHostnameAsync(string hostname, CancellationToken cancellationToken = default);

    Task<CustomDomain?> GetDomainByExternalLinkAsync(
        string provider,
        string externalId,
        string? resourceType = null,
        CancellationToken cancellationToken = default);
}
#pragma warning restore MA0048
