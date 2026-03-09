namespace Incursa.Platform.Dns;

using Incursa.Platform.Dns.Internal;
using Incursa.Platform.Storage;

internal sealed class StorageBackedDnsRecordService : IDnsRecordService
{
    private readonly DnsStorageContext storage;
    private readonly IDnsQueryService queryService;

    public StorageBackedDnsRecordService(DnsStorageContext storage, IDnsQueryService queryService)
    {
        this.storage = storage ?? throw new ArgumentNullException(nameof(storage));
        this.queryService = queryService ?? throw new ArgumentNullException(nameof(queryService));
    }

    public async Task<DnsRecord> UpsertRecordAsync(DnsRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        var normalized = DnsNormalizer.NormalizeRecord(record);
        var previous = await storage.Records.GetAsync(DnsStorageKeys.Record(normalized.Id), cancellationToken).ConfigureAwait(false);
        if (previous is not null)
        {
            normalized = new DnsRecord(
                normalized.Id,
                normalized.ZoneId,
                normalized.Name,
                normalized.Kind,
                normalized.Data,
                normalized.Ttl,
                normalized.Proxied,
                normalized.Comment,
                normalized.CreatedUtc ?? previous.Value.CreatedUtc,
                normalized.ExternalLinks.Count == 0 ? previous.Value.ExternalLinks : normalized.ExternalLinks);
        }

        await storage.Records.WriteAsync(
            DnsStorageKeys.Record(normalized.Id),
            normalized,
            StorageWriteMode.Upsert,
            StorageWriteCondition.Unconditional(),
            cancellationToken).ConfigureAwait(false);

        if (previous is not null)
        {
            await storage.RecordsByZone.DeleteAsync(
                DnsStorageKeys.RecordByZone(previous.Value),
                StorageWriteCondition.Unconditional(),
                cancellationToken).ConfigureAwait(false);
        }

        await storage.RecordsByZone.UpsertAsync(
            DnsStorageKeys.RecordByZone(normalized),
            new RecordByZoneProjection(normalized),
            StorageWriteCondition.Unconditional(),
            cancellationToken).ConfigureAwait(false);

        return normalized;
    }

    public async Task<bool> DeleteRecordAsync(DnsRecordId recordId, CancellationToken cancellationToken = default)
    {
        var existing = await storage.Records.GetAsync(DnsStorageKeys.Record(recordId), cancellationToken).ConfigureAwait(false);
        if (existing is null)
        {
            return false;
        }

        _ = await storage.Records.DeleteAsync(
            DnsStorageKeys.Record(recordId),
            StorageWriteCondition.Unconditional(),
            cancellationToken).ConfigureAwait(false);

        _ = await storage.RecordsByZone.DeleteAsync(
            DnsStorageKeys.RecordByZone(existing.Value),
            StorageWriteCondition.Unconditional(),
            cancellationToken).ConfigureAwait(false);

        return true;
    }

    public async Task<DnsReconcileResult> ReconcileAsync(
        DnsZoneId zoneId,
        IReadOnlyCollection<DnsRecord> desiredRecords,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(desiredRecords);

        var existing = await DnsAsyncEnumerable.ToListAsync(
            queryService.GetRecordsAsync(zoneId, cancellationToken),
            cancellationToken).ConfigureAwait(false);

        var normalizedDesired = desiredRecords
            .Select(DnsNormalizer.NormalizeRecord)
            .Select(item => item.ZoneId == zoneId ? item : throw new InvalidOperationException("All desired records must target the same zone."))
            .ToArray();

        Dictionary<string, DnsRecord> desiredBySignature = new(StringComparer.Ordinal);
        foreach (var record in normalizedDesired)
        {
            _ = desiredBySignature.TryAdd(DnsNormalizer.Signature(record), record);
        }

        var existingBySignature = existing.ToDictionary(DnsNormalizer.Signature, StringComparer.Ordinal);
        List<DnsRecord> upserted = [];
        List<DnsRecord> deleted = [];

        foreach (var pair in desiredBySignature)
        {
            var desired = pair.Value;
            if (existingBySignature.TryGetValue(pair.Key, out var current))
            {
                desired = new DnsRecord(
                    current.Id,
                    desired.ZoneId,
                    desired.Name,
                    desired.Kind,
                    desired.Data,
                    desired.Ttl,
                    desired.Proxied,
                    desired.Comment,
                    current.CreatedUtc,
                    desired.ExternalLinks);
            }

            upserted.Add(await UpsertRecordAsync(desired, cancellationToken).ConfigureAwait(false));
        }

        foreach (var existingRecord in existing)
        {
            if (desiredBySignature.ContainsKey(DnsNormalizer.Signature(existingRecord)))
            {
                continue;
            }

            if (await DeleteRecordAsync(existingRecord.Id, cancellationToken).ConfigureAwait(false))
            {
                deleted.Add(existingRecord);
            }
        }

        return new DnsReconcileResult(upserted, deleted);
    }
}
