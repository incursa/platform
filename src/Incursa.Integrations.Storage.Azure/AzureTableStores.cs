// Copyright (c) Incursa
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Runtime.CompilerServices;
using Azure;
using Azure.Data.Tables;
using Incursa.Platform.Storage;
using Microsoft.Extensions.Logging;

namespace Incursa.Integrations.Storage.Azure;

internal abstract class AzureTableStoreCore<TValue>
{
    private readonly AzureStorageOptions options;
    private readonly AzureStorageJsonSerializer serializer;
    private readonly ILogger logger;
    private readonly TableClient tableClient;
    private readonly SemaphoreSlim ensureGate = new(1, 1);
    private bool ensured;

    protected AzureTableStoreCore(
        AzureStorageClientFactory clientFactory,
        AzureStorageOptions options,
        AzureStorageJsonSerializer serializer,
        ILogger logger,
        string tableName)
    {
        ArgumentNullException.ThrowIfNull(clientFactory);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(serializer);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);

        this.options = options;
        this.serializer = serializer;
        this.logger = logger;
        tableClient = clientFactory.TableServiceClient.GetTableClient(tableName);
    }

    protected async Task<StorageItem<TValue>?> GetAsyncCore(StorageRecordKey key, CancellationToken cancellationToken)
    {
        await EnsureTableReadyAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            NullableResponse<TableEntity> response = await tableClient
                .GetEntityIfExistsAsync<TableEntity>(key.PartitionKey.Value, key.RowKey.Value, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return response.HasValue ? ToStorageItem(response.Value!, key) : null;
        }
        catch (RequestFailedException exception) when (AzureStorageExceptionHelper.IsNotFound(exception))
        {
            return null;
        }
    }

    protected async IAsyncEnumerable<StorageItem<TValue>> QueryPartitionAsyncCore(
        StoragePartitionKey partitionKey,
        StoragePartitionQuery query,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await EnsureTableReadyAsync(cancellationToken).ConfigureAwait(false);

        string filter = BuildFilter(partitionKey, query);
        await foreach (TableEntity entity in tableClient
                           .QueryAsync<TableEntity>(filter, maxPerPage: query.PageSizeHint, cancellationToken: cancellationToken)
                           .WithCancellation(cancellationToken)
                           .ConfigureAwait(false))
        {
            yield return ToStorageItem(entity, new StorageRecordKey(partitionKey, new StorageRowKey(entity.RowKey)));
        }
    }

    protected async Task<StorageItem<TValue>> WriteAsyncCore(
        StorageRecordKey key,
        TValue value,
        StorageWriteMode mode,
        StorageWriteCondition condition,
        CancellationToken cancellationToken)
    {
        await EnsureTableReadyAsync(cancellationToken).ConfigureAwait(false);

        TableEntity entity = CreateEntity(key, value);

        try
        {
            switch (condition.Kind)
            {
                case StorageWriteConditionKind.Unconditional:
                    await tableClient.UpsertEntityAsync(entity, TableUpdateMode.Replace, cancellationToken).ConfigureAwait(false);
                    break;
                case StorageWriteConditionKind.IfNotExists:
                    await tableClient.AddEntityAsync(entity, cancellationToken).ConfigureAwait(false);
                    break;
                case StorageWriteConditionKind.IfMatch:
                    await tableClient.UpdateEntityAsync(
                            entity,
                            AzureStorageConditionMapper.RequireAzureETag(condition),
                            TableUpdateMode.Replace,
                            cancellationToken)
                        .ConfigureAwait(false);
                    break;
                default:
                    throw new StorageOperationNotSupportedException($"Unsupported write condition kind '{condition.Kind}'.");
            }
        }
        catch (RequestFailedException exception) when (AzureStorageExceptionHelper.IsConflictOrPrecondition(exception))
        {
            throw new StoragePreconditionFailedException(
                $"The {mode} operation for '{key}' did not satisfy the requested optimistic concurrency condition.",
                exception);
        }

        StorageItem<TValue>? stored = await GetAsyncCore(key, cancellationToken).ConfigureAwait(false);
        return stored ?? throw new StorageException($"The {mode} operation for '{key}' completed but the entity could not be reloaded.");
    }

    protected async Task<bool> DeleteAsyncCore(
        StorageRecordKey key,
        StorageWriteCondition condition,
        CancellationToken cancellationToken)
    {
        await EnsureTableReadyAsync(cancellationToken).ConfigureAwait(false);

        if (condition.Kind == StorageWriteConditionKind.IfNotExists)
        {
            throw new StorageOperationNotSupportedException("Delete operations do not support IfNotExists preconditions.");
        }

        try
        {
            await tableClient.DeleteEntityAsync(
                    key.PartitionKey.Value,
                    key.RowKey.Value,
                    condition.Kind == StorageWriteConditionKind.IfMatch
                        ? AzureStorageConditionMapper.RequireAzureETag(condition)
                        : ETag.All,
                    cancellationToken)
                .ConfigureAwait(false);

            return true;
        }
        catch (RequestFailedException exception) when (AzureStorageExceptionHelper.IsNotFound(exception) &&
                                                       condition.Kind == StorageWriteConditionKind.Unconditional)
        {
            return false;
        }
        catch (RequestFailedException exception) when (AzureStorageExceptionHelper.IsNotFound(exception) ||
                                                       AzureStorageExceptionHelper.IsConflictOrPrecondition(exception))
        {
            throw new StoragePreconditionFailedException(
                $"The delete operation for '{key}' did not satisfy the requested optimistic concurrency condition.",
                exception);
        }
    }

    protected async Task ExecuteBatchAsyncCore(StorageBatch<TValue> batch, CancellationToken cancellationToken)
    {
        await EnsureTableReadyAsync(cancellationToken).ConfigureAwait(false);

        if (batch.ConsistencyMode != StorageConsistencyMode.SinglePartitionAtomic)
        {
            throw new StorageOperationNotSupportedException(
                "Azure Table batch execution supports only single-partition atomic intent. Cross-partition work must be modeled as eventual propagation.");
        }

        List<TableTransactionAction> actions = new(batch.Operations.Count);
        foreach (StorageBatchOperation<TValue> operation in batch.Operations)
        {
            actions.Add(CreateTransactionAction(operation));
        }

        try
        {
            await tableClient.SubmitTransactionAsync(actions, cancellationToken).ConfigureAwait(false);
        }
        catch (RequestFailedException exception) when (AzureStorageExceptionHelper.IsConflictOrPrecondition(exception))
        {
            throw new StoragePreconditionFailedException(
                "The record batch did not satisfy the requested optimistic concurrency conditions.",
                exception);
        }
    }

    private async Task EnsureTableReadyAsync(CancellationToken cancellationToken)
    {
        if (ensured || !options.CreateResourcesIfMissing)
        {
            return;
        }

        await ensureGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (ensured)
            {
                return;
            }

            logger.LogDebug("Ensuring Azure Table '{TableName}' exists.", tableClient.Name);
            await tableClient.CreateIfNotExistsAsync(cancellationToken).ConfigureAwait(false);
            ensured = true;
        }
        finally
        {
            ensureGate.Release();
        }
    }

    private StorageItem<TValue> ToStorageItem(TableEntity entity, StorageRecordKey key)
    {
        string payload = entity.GetString(AzureStorageExceptionHelper.DataPropertyName)
            ?? throw new StorageException($"The entity '{key}' does not contain the expected data payload.");

        TValue? value = serializer.Deserialize<TValue>(payload);
        return new StorageItem<TValue>(key, value!, new StorageETag(entity.ETag.ToString()), entity.Timestamp);
    }

    private TableEntity CreateEntity(StorageRecordKey key, TValue value)
    {
        return new TableEntity(key.PartitionKey.Value, key.RowKey.Value)
        {
            [AzureStorageExceptionHelper.DataPropertyName] = serializer.SerializeToString(value),
        };
    }

    private TableTransactionAction CreateTransactionAction(StorageBatchOperation<TValue> operation)
    {
        if (operation.Kind == StorageBatchOperationKind.Delete)
        {
            if (operation.Condition.Kind == StorageWriteConditionKind.IfNotExists)
            {
                throw new StorageOperationNotSupportedException("Delete batch operations do not support IfNotExists preconditions.");
            }

            TableEntity deleteEntity = new(operation.Key.PartitionKey.Value, operation.Key.RowKey.Value);
            ETag deleteTag = operation.Condition.Kind == StorageWriteConditionKind.IfMatch
                ? AzureStorageConditionMapper.RequireAzureETag(operation.Condition)
                : ETag.All;

            return new TableTransactionAction(TableTransactionActionType.Delete, deleteEntity, deleteTag);
        }

        if (operation.Value is null)
        {
            throw new StorageException("Batch write operations require a value.");
        }

        TableEntity entity = CreateEntity(operation.Key, operation.Value);
        return operation.Condition.Kind switch
        {
            StorageWriteConditionKind.IfNotExists => new TableTransactionAction(TableTransactionActionType.Add, entity),
            StorageWriteConditionKind.IfMatch => new TableTransactionAction(
                TableTransactionActionType.UpdateReplace,
                entity,
                AzureStorageConditionMapper.RequireAzureETag(operation.Condition)),
            StorageWriteConditionKind.Unconditional => new TableTransactionAction(TableTransactionActionType.UpsertReplace, entity),
            _ => throw new StorageOperationNotSupportedException($"Unsupported batch write condition kind '{operation.Condition.Kind}'."),
        };
    }

    private static string BuildFilter(StoragePartitionKey partitionKey, StoragePartitionQuery query)
    {
        string partitionFilter = $"PartitionKey eq '{EscapeFilterValue(partitionKey.Value)}'";

        return query.Mode switch
        {
            StoragePartitionQueryMode.All => partitionFilter,
            StoragePartitionQueryMode.Prefix =>
                $"{partitionFilter} and RowKey ge '{EscapeFilterValue(query.RowKeyPrefix!)}' and RowKey lt '{EscapeFilterValue(GetPrefixUpperBound(query.RowKeyPrefix!))}'",
            StoragePartitionQueryMode.Range => BuildRangeFilter(partitionFilter, query),
            _ => partitionFilter,
        };
    }

    private static string BuildRangeFilter(string partitionFilter, StoragePartitionQuery query)
    {
        List<string> parts = new() { partitionFilter };
        if (query.StartRowKeyInclusive is not null)
        {
            parts.Add($"RowKey ge '{EscapeFilterValue(query.StartRowKeyInclusive)}'");
        }

        if (query.EndRowKeyExclusive is not null)
        {
            parts.Add($"RowKey lt '{EscapeFilterValue(query.EndRowKeyExclusive)}'");
        }

        return string.Join(" and ", parts);
    }

    private static string EscapeFilterValue(string value) => value.Replace("'", "''", StringComparison.Ordinal);

    private static string GetPrefixUpperBound(string prefix) => prefix + '\uffff';
}

internal sealed class AzureTableRecordStore<TRecord> : AzureTableStoreCore<TRecord>, IRecordStore<TRecord>
{
    public AzureTableRecordStore(
        AzureStorageClientFactory clientFactory,
        AzureStorageNameResolver nameResolver,
        AzureStorageOptions options,
        AzureStorageJsonSerializer serializer,
        ILogger<AzureTableRecordStore<TRecord>> logger)
        : base(clientFactory, options, serializer, logger, nameResolver.GetRecordTableName(typeof(TRecord)))
    {
    }

    public Task<StorageItem<TRecord>?> GetAsync(StorageRecordKey key, CancellationToken cancellationToken = default) =>
        GetAsyncCore(key, cancellationToken);

    public IAsyncEnumerable<StorageItem<TRecord>> QueryPartitionAsync(
        StoragePartitionKey partitionKey,
        StoragePartitionQuery query,
        CancellationToken cancellationToken = default) =>
        QueryPartitionAsyncCore(partitionKey, query, cancellationToken);

    public Task<StorageItem<TRecord>> WriteAsync(
        StorageRecordKey key,
        TRecord value,
        StorageWriteMode mode,
        StorageWriteCondition condition,
        CancellationToken cancellationToken = default) =>
        WriteAsyncCore(key, value, mode, condition, cancellationToken);

    public Task<bool> DeleteAsync(
        StorageRecordKey key,
        StorageWriteCondition condition,
        CancellationToken cancellationToken = default) =>
        DeleteAsyncCore(key, condition, cancellationToken);

    public Task ExecuteBatchAsync(StorageBatch<TRecord> batch, CancellationToken cancellationToken = default) =>
        ExecuteBatchAsyncCore(batch, cancellationToken);
}

internal sealed class AzureTableLookupStore<TLookup> : AzureTableStoreCore<TLookup>, ILookupStore<TLookup>
{
    public AzureTableLookupStore(
        AzureStorageClientFactory clientFactory,
        AzureStorageNameResolver nameResolver,
        AzureStorageOptions options,
        AzureStorageJsonSerializer serializer,
        ILogger<AzureTableLookupStore<TLookup>> logger)
        : base(clientFactory, options, serializer, logger, nameResolver.GetLookupTableName(typeof(TLookup)))
    {
    }

    public Task<StorageItem<TLookup>?> GetAsync(StorageRecordKey key, CancellationToken cancellationToken = default) =>
        GetAsyncCore(key, cancellationToken);

    public IAsyncEnumerable<StorageItem<TLookup>> QueryPartitionAsync(
        StoragePartitionKey partitionKey,
        StoragePartitionQuery query,
        CancellationToken cancellationToken = default) =>
        QueryPartitionAsyncCore(partitionKey, query, cancellationToken);

    public Task<StorageItem<TLookup>> UpsertAsync(
        StorageRecordKey key,
        TLookup value,
        StorageWriteCondition condition,
        CancellationToken cancellationToken = default) =>
        WriteAsyncCore(key, value, StorageWriteMode.Upsert, condition, cancellationToken);

    public Task<bool> DeleteAsync(
        StorageRecordKey key,
        StorageWriteCondition condition,
        CancellationToken cancellationToken = default) =>
        DeleteAsyncCore(key, condition, cancellationToken);
}
