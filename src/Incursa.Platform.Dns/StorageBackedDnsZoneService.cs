namespace Incursa.Platform.Dns;

using Incursa.Platform.Dns.Internal;
using Incursa.Platform.Storage;

internal sealed class StorageBackedDnsZoneService : IDnsZoneService
{
    private readonly DnsStorageContext storage;

    public StorageBackedDnsZoneService(DnsStorageContext storage)
    {
        this.storage = storage ?? throw new ArgumentNullException(nameof(storage));
    }

    public async Task<DnsZone> UpsertZoneAsync(DnsZone zone, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(zone);

        var normalized = DnsNormalizer.NormalizeZone(zone);
        var previous = await storage.Zones.GetAsync(DnsStorageKeys.Zone(normalized.Id), cancellationToken).ConfigureAwait(false);
        if (previous is not null)
        {
            normalized = new DnsZone(
                normalized.Id,
                normalized.Name,
                normalized.Owner,
                normalized.CreatedUtc ?? previous.Value.CreatedUtc,
                normalized.ExternalLinks.Count == 0 ? previous.Value.ExternalLinks : normalized.ExternalLinks);
        }

        await storage.Zones.WriteAsync(
            DnsStorageKeys.Zone(normalized.Id),
            normalized,
            StorageWriteMode.Upsert,
            StorageWriteCondition.Unconditional(),
            cancellationToken).ConfigureAwait(false);

        if (previous?.Value.Owner is not null)
        {
            await storage.ZonesByOwner.DeleteAsync(
                DnsStorageKeys.ZoneByOwner(previous.Value.Owner, previous.Value.Id),
                StorageWriteCondition.Unconditional(),
                cancellationToken).ConfigureAwait(false);
        }

        if (normalized.Owner is not null)
        {
            await storage.ZonesByOwner.UpsertAsync(
                DnsStorageKeys.ZoneByOwner(normalized.Owner, normalized.Id),
                new ZoneByOwnerProjection(normalized),
                StorageWriteCondition.Unconditional(),
                cancellationToken).ConfigureAwait(false);
        }

        return normalized;
    }
}
