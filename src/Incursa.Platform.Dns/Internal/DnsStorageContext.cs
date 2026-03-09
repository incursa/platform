namespace Incursa.Platform.Dns.Internal;

using Incursa.Platform.Storage;

internal sealed class DnsStorageContext
{
    public DnsStorageContext(
        IRecordStore<DnsZone> zones,
        IRecordStore<DnsRecord> records,
        ILookupStore<ZoneByOwnerProjection> zonesByOwner,
        ILookupStore<RecordByZoneProjection> recordsByZone)
    {
        Zones = zones;
        Records = records;
        ZonesByOwner = zonesByOwner;
        RecordsByZone = recordsByZone;
    }

    public IRecordStore<DnsZone> Zones { get; }

    public IRecordStore<DnsRecord> Records { get; }

    public ILookupStore<ZoneByOwnerProjection> ZonesByOwner { get; }

    public ILookupStore<RecordByZoneProjection> RecordsByZone { get; }
}
