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

namespace Incursa.Platform.Storage;

/// <summary>
/// Represents a record-batch mutation request.
/// </summary>
/// <typeparam name="TRecord">The record type.</typeparam>
public sealed record StorageBatch<TRecord>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StorageBatch{TRecord}"/> class.
    /// </summary>
    /// <param name="operations">The operations in the batch.</param>
    /// <param name="consistencyMode">The consistency intent for the batch.</param>
    public StorageBatch(
        IReadOnlyList<StorageBatchOperation<TRecord>> operations,
        StorageConsistencyMode consistencyMode = StorageConsistencyMode.SinglePartitionAtomic)
    {
        ArgumentNullException.ThrowIfNull(operations);

        if (operations.Count == 0)
        {
            throw new ArgumentException("A batch must contain at least one operation.", nameof(operations));
        }

        if (consistencyMode == StorageConsistencyMode.SinglePartitionAtomic)
        {
            var partition = operations[0].Key.PartitionKey;
            if (operations.Any(operation => operation.Key.PartitionKey != partition))
            {
                throw new ArgumentException(
                    "Single-partition atomic batches must target exactly one partition.",
                    nameof(operations));
            }
        }

        Operations = operations;
        ConsistencyMode = consistencyMode;
    }

    /// <summary>
    /// Gets the requested consistency intent.
    /// </summary>
    public StorageConsistencyMode ConsistencyMode { get; }

    /// <summary>
    /// Gets the ordered operations in the batch.
    /// </summary>
    public IReadOnlyList<StorageBatchOperation<TRecord>> Operations { get; }
}

/// <summary>
/// Represents an individual operation inside a record batch.
/// </summary>
/// <typeparam name="TRecord">The record type.</typeparam>
public sealed record StorageBatchOperation<TRecord>
{
    private StorageBatchOperation(
        StorageBatchOperationKind kind,
        StorageRecordKey key,
        TRecord? value,
        StorageWriteCondition condition)
    {
        if (kind != StorageBatchOperationKind.Delete && value is null)
        {
            throw new ArgumentNullException(nameof(value), "Non-delete batch operations require a value.");
        }

        Kind = kind;
        Key = key;
        Value = value;
        Condition = condition;
    }

    /// <summary>
    /// Gets the operation kind.
    /// </summary>
    public StorageBatchOperationKind Kind { get; }

    /// <summary>
    /// Gets the target key.
    /// </summary>
    public StorageRecordKey Key { get; }

    /// <summary>
    /// Gets the mutation value for put and upsert operations.
    /// </summary>
    public TRecord? Value { get; }

    /// <summary>
    /// Gets the write condition for the operation.
    /// </summary>
    public StorageWriteCondition Condition { get; }

    /// <summary>
    /// Creates a put operation.
    /// </summary>
    /// <param name="key">The target key.</param>
    /// <param name="value">The replacement value.</param>
    /// <param name="condition">The optimistic concurrency condition.</param>
    /// <returns>The batch operation.</returns>
    public static StorageBatchOperation<TRecord> Put(
        StorageRecordKey key,
        TRecord value,
        StorageWriteCondition condition) =>
        new(StorageBatchOperationKind.Put, key, value, condition);

    /// <summary>
    /// Creates an upsert operation.
    /// </summary>
    /// <param name="key">The target key.</param>
    /// <param name="value">The value to insert or replace.</param>
    /// <param name="condition">The optimistic concurrency condition.</param>
    /// <returns>The batch operation.</returns>
    public static StorageBatchOperation<TRecord> Upsert(
        StorageRecordKey key,
        TRecord value,
        StorageWriteCondition condition) =>
        new(StorageBatchOperationKind.Upsert, key, value, condition);

    /// <summary>
    /// Creates a delete operation.
    /// </summary>
    /// <param name="key">The target key.</param>
    /// <param name="condition">The optimistic concurrency condition.</param>
    /// <returns>The batch operation.</returns>
    public static StorageBatchOperation<TRecord> Delete(
        StorageRecordKey key,
        StorageWriteCondition condition) =>
        new(StorageBatchOperationKind.Delete, key, default, condition);
}

/// <summary>
/// Represents the supported record batch operation kinds.
/// </summary>
public enum StorageBatchOperationKind
{
    /// <summary>
    /// Replaces the current record at the target key.
    /// </summary>
    Put = 0,

    /// <summary>
    /// Inserts or replaces the current record at the target key.
    /// </summary>
    Upsert = 1,

    /// <summary>
    /// Deletes the current record at the target key.
    /// </summary>
    Delete = 2,
}
