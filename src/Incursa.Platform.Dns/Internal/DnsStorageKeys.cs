namespace Incursa.Platform.Dns.Internal;

using Incursa.Platform.Storage;

internal static class DnsStorageKeys
{
    public static StorageRecordKey Zone(DnsZoneId zoneId) =>
        new(new StoragePartitionKey("dns-zone"), new StorageRowKey(zoneId.Value));

    public static StorageRecordKey Record(DnsRecordId recordId) =>
        new(new StoragePartitionKey("dns-record"), new StorageRowKey(recordId.Value));

    public static StoragePartitionKey ZoneByOwnerPartition(string owner) =>
        new("dns-zone-by-owner/" + owner.Trim().ToLowerInvariant());

    public static StorageRecordKey ZoneByOwner(string owner, DnsZoneId zoneId) =>
        new(ZoneByOwnerPartition(owner), new StorageRowKey(zoneId.Value));

    public static StoragePartitionKey RecordByZonePartition(DnsZoneId zoneId) =>
        new("dns-record-by-zone/" + zoneId.Value);

    public static StorageRecordKey RecordByZone(DnsRecord record)
    {
        var normalized = DnsNormalizer.NormalizeRecord(record);
        return new(
            RecordByZonePartition(normalized.ZoneId),
            new StorageRowKey(
                normalized.Name + "|" + normalized.Kind.ToString().ToUpperInvariant() + "|" + normalized.Id.Value));
    }

    public static string RecordPrefix(string? name, DnsRecordKind? kind)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }

        var normalizedName = DnsNormalizer.NormalizeDomainName(name);
        return kind is null
            ? normalizedName + "|"
            : normalizedName + "|" + kind.Value.ToString().ToUpperInvariant() + "|";
    }
}
