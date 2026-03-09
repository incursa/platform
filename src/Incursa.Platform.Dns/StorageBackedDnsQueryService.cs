namespace Incursa.Platform.Dns;

using System.Runtime.CompilerServices;
using Incursa.Platform.Dns.Internal;
using Incursa.Platform.Storage;

internal sealed class StorageBackedDnsQueryService : IDnsQueryService
{
    private readonly DnsStorageContext storage;

    public StorageBackedDnsQueryService(DnsStorageContext storage)
    {
        this.storage = storage ?? throw new ArgumentNullException(nameof(storage));
    }

    public async Task<DnsZone?> GetZoneAsync(DnsZoneId zoneId, CancellationToken cancellationToken = default)
    {
        var item = await storage.Zones.GetAsync(DnsStorageKeys.Zone(zoneId), cancellationToken).ConfigureAwait(false);
        return item?.Value;
    }

    public async Task<DnsRecord?> GetRecordAsync(DnsRecordId recordId, CancellationToken cancellationToken = default)
    {
        var item = await storage.Records.GetAsync(DnsStorageKeys.Record(recordId), cancellationToken).ConfigureAwait(false);
        return item?.Value;
    }

    public async IAsyncEnumerable<DnsZone> QueryZonesByOwnerAsync(
        string owner,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(owner);

        await foreach (var item in storage.ZonesByOwner.QueryPartitionAsync(
                           DnsStorageKeys.ZoneByOwnerPartition(owner),
                           StoragePartitionQuery.All(),
                           cancellationToken).ConfigureAwait(false))
        {
            yield return item.Value.Zone;
        }
    }

    public async IAsyncEnumerable<DnsRecord> GetRecordsAsync(
        DnsZoneId zoneId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var item in storage.RecordsByZone.QueryPartitionAsync(
                           DnsStorageKeys.RecordByZonePartition(zoneId),
                           StoragePartitionQuery.All(),
                           cancellationToken).ConfigureAwait(false))
        {
            yield return item.Value.Record;
        }
    }

    public async IAsyncEnumerable<DnsRecord> QueryRecordsAsync(
        DnsRecordQuery query,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        IAsyncEnumerable<StorageItem<RecordByZoneProjection>> source;
        if (string.IsNullOrWhiteSpace(query.Name))
        {
            source = storage.RecordsByZone.QueryPartitionAsync(
                DnsStorageKeys.RecordByZonePartition(query.ZoneId),
                StoragePartitionQuery.All(),
                cancellationToken);
        }
        else
        {
            source = storage.RecordsByZone.QueryPartitionAsync(
                DnsStorageKeys.RecordByZonePartition(query.ZoneId),
                StoragePartitionQuery.WithPrefix(DnsStorageKeys.RecordPrefix(query.Name, query.Kind)),
                cancellationToken);
        }

        await foreach (var item in source.ConfigureAwait(false))
        {
            if (!string.IsNullOrWhiteSpace(query.Name)
                && !string.Equals(
                    DnsNormalizer.NormalizeDomainName(query.Name),
                    item.Value.Record.Name,
                    StringComparison.Ordinal))
            {
                continue;
            }

            if (query.Kind is not null && item.Value.Record.Kind != query.Kind.Value)
            {
                continue;
            }

            yield return item.Value.Record;
        }
    }
}
