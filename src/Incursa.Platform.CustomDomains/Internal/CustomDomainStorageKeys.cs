namespace Incursa.Platform.CustomDomains.Internal;

using Incursa.Platform.Storage;

internal static class CustomDomainStorageKeys
{
    public static StorageRecordKey Domain(CustomDomainId domainId) =>
        new(new StoragePartitionKey("custom-domain"), new StorageRowKey(domainId.Value));

    public static StorageRecordKey DomainByHostname(string hostname) =>
        new(
            new StoragePartitionKey("custom-domain-by-hostname"),
            new StorageRowKey(CustomDomainNormalizer.NormalizeHostname(hostname)));

    public static StorageRecordKey DomainByExternalLink(
        string provider,
        string externalId,
        string? resourceType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);
        ArgumentException.ThrowIfNullOrWhiteSpace(externalId);

        var normalizedProvider = provider.Trim().ToLowerInvariant();
        var normalizedResourceType = string.IsNullOrWhiteSpace(resourceType)
            ? "__default"
            : resourceType.Trim().ToLowerInvariant();

        return new(
            new StoragePartitionKey("custom-domain-by-external-link/" + normalizedProvider + "/" + normalizedResourceType),
            new StorageRowKey(Uri.EscapeDataString(externalId.Trim())));
    }
}
