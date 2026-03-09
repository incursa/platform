#pragma warning disable MA0048
namespace Incursa.Platform.Dns;

public interface IDnsZoneService
{
    Task<DnsZone> UpsertZoneAsync(DnsZone zone, CancellationToken cancellationToken = default);
}

public interface IDnsRecordService
{
    Task<DnsRecord> UpsertRecordAsync(DnsRecord record, CancellationToken cancellationToken = default);

    Task<bool> DeleteRecordAsync(DnsRecordId recordId, CancellationToken cancellationToken = default);

    Task<DnsReconcileResult> ReconcileAsync(
        DnsZoneId zoneId,
        IReadOnlyCollection<DnsRecord> desiredRecords,
        CancellationToken cancellationToken = default);
}

public interface IDnsQueryService
{
    Task<DnsZone?> GetZoneAsync(DnsZoneId zoneId, CancellationToken cancellationToken = default);

    Task<DnsRecord?> GetRecordAsync(DnsRecordId recordId, CancellationToken cancellationToken = default);

    IAsyncEnumerable<DnsZone> QueryZonesByOwnerAsync(string owner, CancellationToken cancellationToken = default);

    IAsyncEnumerable<DnsRecord> GetRecordsAsync(DnsZoneId zoneId, CancellationToken cancellationToken = default);

    IAsyncEnumerable<DnsRecord> QueryRecordsAsync(DnsRecordQuery query, CancellationToken cancellationToken = default);
}
#pragma warning restore MA0048
