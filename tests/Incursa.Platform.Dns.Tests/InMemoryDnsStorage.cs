#pragma warning disable MA0048
namespace Incursa.Platform.Dns.Tests;

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
#pragma warning restore MA0048
