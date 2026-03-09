namespace Incursa.Platform.Access;

using System.Runtime.CompilerServices;
using Incursa.Platform.Access.Internal;
using Incursa.Platform.Storage;

internal sealed class StorageBackedAccessAuditJournal : IAccessAuditJournal
{
    private readonly AccessStorageContext storage;

    public StorageBackedAccessAuditJournal(AccessStorageContext storage)
    {
        this.storage = storage ?? throw new ArgumentNullException(nameof(storage));
    }

    public async Task AppendAsync(AccessAuditEntry entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        await storage.AuditEntries.WriteAsync(
            AccessStorageKeys.AuditEntry(entry.Id),
            entry,
            StorageWriteMode.Upsert,
            StorageWriteCondition.Unconditional(),
            cancellationToken).ConfigureAwait(false);

        var userId = entry.SubjectUserId ?? entry.ActorUserId;
        if (userId is not null)
        {
            await storage.AuditEntriesByUser.UpsertAsync(
                AccessStorageKeys.AuditByUser((AccessUserId)userId, entry.OccurredUtc, entry.Id),
                new AuditEntryByUserProjection(entry),
                StorageWriteCondition.Unconditional(),
                cancellationToken).ConfigureAwait(false);
        }

        if (entry.Resource is not null)
        {
            await storage.AuditEntriesByResource.UpsertAsync(
                AccessStorageKeys.AuditByResource(entry.Resource, entry.OccurredUtc, entry.Id),
                new AuditEntryByResourceProjection(entry),
                StorageWriteCondition.Unconditional(),
                cancellationToken).ConfigureAwait(false);
        }
    }

    public async IAsyncEnumerable<AccessAuditEntry> QueryByUserAsync(
        AccessUserId userId,
        DateTimeOffset? occurredAfterUtc = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var query = occurredAfterUtc is null
            ? StoragePartitionQuery.All()
            : StoragePartitionQuery.WithinRange(
                AccessStorageKeys.AuditLookupRow((DateTimeOffset)occurredAfterUtc, new AccessAuditEntryId("0")),
                null);

        await foreach (var item in storage.AuditEntriesByUser.QueryPartitionAsync(
                           AccessStorageKeys.AuditByUserPartition(userId),
                           query,
                           cancellationToken).ConfigureAwait(false))
        {
            yield return item.Value.Entry;
        }
    }

    public async IAsyncEnumerable<AccessAuditEntry> QueryByResourceAsync(
        AccessResourceReference resource,
        DateTimeOffset? occurredAfterUtc = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(resource);

        var query = occurredAfterUtc is null
            ? StoragePartitionQuery.All()
            : StoragePartitionQuery.WithinRange(
                AccessStorageKeys.AuditLookupRow((DateTimeOffset)occurredAfterUtc, new AccessAuditEntryId("0")),
                null);

        await foreach (var item in storage.AuditEntriesByResource.QueryPartitionAsync(
                           AccessStorageKeys.AuditByResourcePartition(resource),
                           query,
                           cancellationToken).ConfigureAwait(false))
        {
            yield return item.Value.Entry;
        }
    }
}
