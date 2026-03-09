#pragma warning disable MA0048
namespace Incursa.Platform.Dns.Internal;

internal sealed record ZoneByOwnerProjection(DnsZone Zone);

internal sealed record RecordByZoneProjection(DnsRecord Record);
#pragma warning restore MA0048
