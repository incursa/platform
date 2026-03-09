#pragma warning disable MA0048
namespace Incursa.Platform.Access.Tests;

using System.Collections.Concurrent;
using System.Globalization;
using System.Runtime.CompilerServices;
using Incursa.Platform.Storage;

internal static class InMemoryStorageQuery
{
    public static bool Matches(StoragePartitionQuery query, string rowKey) =>
        query.Mode switch
        {
            StoragePartitionQueryMode.All => true,
            StoragePartitionQueryMode.Prefix => rowKey.StartsWith(query.RowKeyPrefix!, StringComparison.Ordinal),
            StoragePartitionQueryMode.Range => (query.StartRowKeyInclusive is null
                    || string.Compare(rowKey, query.StartRowKeyInclusive, StringComparison.Ordinal) >= 0)
                && (query.EndRowKeyExclusive is null
                    || string.Compare(rowKey, query.EndRowKeyExclusive, StringComparison.Ordinal) < 0),
            _ => throw new InvalidOperationException("Unknown query mode."),
        };
}

internal sealed class InMemoryRecordStore<TRecord> : IRecordStore<TRecord>
    where TRecord : class
{
    private readonly ConcurrentDictionary<StorageRecordKey, StorageItem<TRecord>> items = new();
    private int etagSequence;

    public Task<StorageItem<TRecord>?> GetAsync(StorageRecordKey key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(items.TryGetValue(key, out var item) ? item : null);
    }

    public async IAsyncEnumerable<StorageItem<TRecord>> QueryPartitionAsync(
        StoragePartitionKey partitionKey,
        StoragePartitionQuery query,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        foreach (var item in items.Values
                     .Where(item => item.Key.PartitionKey == partitionKey)
                     .OrderBy(static item => item.Key.RowKey.Value, StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!InMemoryStorageQuery.Matches(query, item.Key.RowKey.Value))
            {
                continue;
            }

            yield return item;
            await Task.CompletedTask.ConfigureAwait(false);
        }
    }

    public Task<StorageItem<TRecord>> WriteAsync(
        StorageRecordKey key,
        TRecord value,
        StorageWriteMode mode,
        StorageWriteCondition condition,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ApplyWriteCondition(key, condition);

        var item = new StorageItem<TRecord>(
            key,
            value,
            new StorageETag("etag-" + Interlocked.Increment(ref etagSequence).ToString(CultureInfo.InvariantCulture)),
            DateTimeOffset.UtcNow);
        items[key] = item;
        return Task.FromResult(item);
    }

    public Task<bool> DeleteAsync(
        StorageRecordKey key,
        StorageWriteCondition condition,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ApplyWriteCondition(key, condition, allowMissingOnDelete: true);
        return Task.FromResult(items.TryRemove(key, out _));
    }

    public async Task ExecuteBatchAsync(StorageBatch<TRecord> batch, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(batch);

        foreach (var operation in batch.Operations)
        {
            switch (operation.Kind)
            {
                case StorageBatchOperationKind.Put:
                    await WriteAsync(
                        operation.Key,
                        operation.Value!,
                        StorageWriteMode.Put,
                        operation.Condition,
                        cancellationToken).ConfigureAwait(false);
                    break;
                case StorageBatchOperationKind.Upsert:
                    await WriteAsync(
                        operation.Key,
                        operation.Value!,
                        StorageWriteMode.Upsert,
                        operation.Condition,
                        cancellationToken).ConfigureAwait(false);
                    break;
                case StorageBatchOperationKind.Delete:
                    await DeleteAsync(operation.Key, operation.Condition, cancellationToken).ConfigureAwait(false);
                    break;
                default:
                    throw new InvalidOperationException("Unknown batch operation.");
            }
        }
    }

    private void ApplyWriteCondition(
        StorageRecordKey key,
        StorageWriteCondition condition,
        bool allowMissingOnDelete = false)
    {
        items.TryGetValue(key, out var existing);

        switch (condition.Kind)
        {
            case StorageWriteConditionKind.Unconditional:
                return;
            case StorageWriteConditionKind.IfNotExists:
                if (existing is not null)
                {
                    throw new InvalidOperationException("The record already exists.");
                }

                return;
            case StorageWriteConditionKind.IfMatch:
                if (existing is null)
                {
                    if (allowMissingOnDelete)
                    {
                        return;
                    }

                    throw new InvalidOperationException("The record does not exist.");
                }

                if (existing.ETag != condition.ETag)
                {
                    throw new InvalidOperationException("The ETag does not match.");
                }

                return;
            default:
                throw new InvalidOperationException("Unknown write condition.");
        }
    }
}

internal sealed class InMemoryLookupStore<TLookup> : ILookupStore<TLookup>
    where TLookup : class
{
    private readonly ConcurrentDictionary<StorageRecordKey, StorageItem<TLookup>> items = new();
    private int etagSequence;

    public Task<StorageItem<TLookup>?> GetAsync(StorageRecordKey key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(items.TryGetValue(key, out var item) ? item : null);
    }

    public async IAsyncEnumerable<StorageItem<TLookup>> QueryPartitionAsync(
        StoragePartitionKey partitionKey,
        StoragePartitionQuery query,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        foreach (var item in items.Values
                     .Where(item => item.Key.PartitionKey == partitionKey)
                     .OrderBy(static item => item.Key.RowKey.Value, StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!InMemoryStorageQuery.Matches(query, item.Key.RowKey.Value))
            {
                continue;
            }

            yield return item;
            await Task.CompletedTask.ConfigureAwait(false);
        }
    }

    public Task<StorageItem<TLookup>> UpsertAsync(
        StorageRecordKey key,
        TLookup value,
        StorageWriteCondition condition,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var item = new StorageItem<TLookup>(
            key,
            value,
            new StorageETag("etag-" + Interlocked.Increment(ref etagSequence).ToString(CultureInfo.InvariantCulture)),
            DateTimeOffset.UtcNow);
        items[key] = item;
        return Task.FromResult(item);
    }

    public Task<bool> DeleteAsync(
        StorageRecordKey key,
        StorageWriteCondition condition,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(items.TryRemove(key, out _));
    }
}

internal sealed class InMemoryWorkStore<TWorkItem> : IWorkStore<TWorkItem>
{
    private readonly Queue<WorkItem<TWorkItem>> queue = new();
    private readonly Dictionary<string, ClaimedWorkItem<TWorkItem>> claimedItems = new(StringComparer.Ordinal);

    public IReadOnlyCollection<WorkItem<TWorkItem>> EnqueuedItems
    {
        get
        {
            lock (queue)
            {
                return queue.ToArray();
            }
        }
    }

    public Task EnqueueAsync(
        WorkItem<TWorkItem> item,
        WorkEnqueueOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        cancellationToken.ThrowIfCancellationRequested();

        lock (queue)
        {
            queue.Enqueue(item);
        }

        return Task.CompletedTask;
    }

    public Task<ClaimedWorkItem<TWorkItem>?> ClaimAsync(
        WorkClaimOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        cancellationToken.ThrowIfCancellationRequested();

        lock (queue)
        {
            if (queue.Count == 0)
            {
                return Task.FromResult<ClaimedWorkItem<TWorkItem>?>(null);
            }

            var item = queue.Dequeue();
            var claim = new ClaimedWorkItem<TWorkItem>(
                item,
                new WorkClaimToken("claim-" + Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture)),
                DateTimeOffset.UtcNow.Add(options.VisibilityTimeout),
                1);
            claimedItems[claim.ClaimToken.Value] = claim;
            return Task.FromResult<ClaimedWorkItem<TWorkItem>?>(claim);
        }
    }

    public Task CompleteAsync(WorkClaimToken claimToken, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        claimedItems.Remove(claimToken.Value);
        return Task.CompletedTask;
    }

    public Task AbandonAsync(
        WorkClaimToken claimToken,
        WorkReleaseOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!claimedItems.Remove(claimToken.Value, out var claim))
        {
            return Task.CompletedTask;
        }

        lock (queue)
        {
            queue.Enqueue(claim.Item);
        }

        return Task.CompletedTask;
    }
}

internal sealed class InMemoryCoordinationStore : ICoordinationStore
{
    private readonly ConcurrentDictionary<StorageRecordKey, object> checkpoints = new();
    private readonly ConcurrentDictionary<StorageRecordKey, byte> idempotencyMarkers = new();
    private readonly ConcurrentDictionary<StorageRecordKey, CoordinationLease> leases = new();

    public Task<bool> TryCreateIdempotencyMarkerAsync(
        StorageRecordKey key,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(idempotencyMarkers.TryAdd(key, 0));
    }

    public Task<bool> IdempotencyMarkerExistsAsync(
        StorageRecordKey key,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(idempotencyMarkers.ContainsKey(key));
    }

    public Task<CoordinationLease?> TryAcquireLeaseAsync(
        CoordinationLeaseRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var lease = new CoordinationLease(
            request.Key,
            request.Owner,
            new CoordinationLeaseToken(Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture)),
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.Add(request.Duration),
            request.Duration);

        if (!leases.TryAdd(request.Key, lease))
        {
            return Task.FromResult<CoordinationLease?>(null);
        }

        return Task.FromResult<CoordinationLease?>(lease);
    }

    public Task<CoordinationLease> RenewLeaseAsync(
        CoordinationLease lease,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(lease);
        cancellationToken.ThrowIfCancellationRequested();

        var renewed = lease with { ExpiresUtc = DateTimeOffset.UtcNow.Add(lease.Duration) };
        leases[lease.Key] = renewed;
        return Task.FromResult(renewed);
    }

    public Task ReleaseLeaseAsync(CoordinationLease lease, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(lease);
        cancellationToken.ThrowIfCancellationRequested();
        leases.TryRemove(lease.Key, out _);
        return Task.CompletedTask;
    }

    public Task<StorageItem<TState>?> GetCheckpointAsync<TState>(
        StorageRecordKey key,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!checkpoints.TryGetValue(key, out var state))
        {
            return Task.FromResult<StorageItem<TState>?>(null);
        }

        return Task.FromResult<StorageItem<TState>?>(
            new StorageItem<TState>(key, (TState)state, new StorageETag("checkpoint"), DateTimeOffset.UtcNow));
    }

    public Task<StorageItem<TState>> WriteCheckpointAsync<TState>(
        StorageRecordKey key,
        TState state,
        StorageWriteCondition condition,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        checkpoints[key] = state!;
        return Task.FromResult(new StorageItem<TState>(key, state, new StorageETag("checkpoint"), DateTimeOffset.UtcNow));
    }
}
#pragma warning restore MA0048
