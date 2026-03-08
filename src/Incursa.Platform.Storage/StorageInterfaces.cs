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
/// Provides partition-aware record storage for write-heavy entities.
/// </summary>
/// <typeparam name="TRecord">The record type.</typeparam>
public interface IRecordStore<TRecord>
{
    /// <summary>
    /// Reads a record by exact partition key and row key.
    /// </summary>
    Task<StorageItem<TRecord>?> GetAsync(StorageRecordKey key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Scans a single partition using one of the supported row-key query shapes.
    /// </summary>
    IAsyncEnumerable<StorageItem<TRecord>> QueryPartitionAsync(
        StoragePartitionKey partitionKey,
        StoragePartitionQuery query,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes a record by exact key using the supplied write mode and optimistic concurrency condition.
    /// </summary>
    Task<StorageItem<TRecord>> WriteAsync(
        StorageRecordKey key,
        TRecord value,
        StorageWriteMode mode,
        StorageWriteCondition condition,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a record by exact key using the supplied optimistic concurrency condition.
    /// </summary>
    Task<bool> DeleteAsync(
        StorageRecordKey key,
        StorageWriteCondition condition,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a record batch.
    /// </summary>
    Task ExecuteBatchAsync(StorageBatch<TRecord> batch, CancellationToken cancellationToken = default);
}

/// <summary>
/// Provides a simpler projection-oriented storage abstraction.
/// </summary>
/// <typeparam name="TLookup">The projection type.</typeparam>
public interface ILookupStore<TLookup>
{
    /// <summary>
    /// Reads a projection by exact partition key and row key.
    /// </summary>
    Task<StorageItem<TLookup>?> GetAsync(StorageRecordKey key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Scans a single partition using one of the supported row-key query shapes.
    /// </summary>
    IAsyncEnumerable<StorageItem<TLookup>> QueryPartitionAsync(
        StoragePartitionKey partitionKey,
        StoragePartitionQuery query,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts or replaces a projection by exact key.
    /// </summary>
    Task<StorageItem<TLookup>> UpsertAsync(
        StorageRecordKey key,
        TLookup value,
        StorageWriteCondition condition,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a projection by exact key.
    /// </summary>
    Task<bool> DeleteAsync(
        StorageRecordKey key,
        StorageWriteCondition condition,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Provides typed JSON and raw-binary payload storage.
/// </summary>
public interface IPayloadStore
{
    /// <summary>
    /// Writes a typed JSON payload.
    /// </summary>
    Task<PayloadMetadata> WriteJsonAsync<TPayload>(
        StoragePayloadKey key,
        TPayload value,
        PayloadWriteOptions? options = null,
        StorageWriteCondition condition = default,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads a typed JSON payload.
    /// </summary>
    Task<PayloadReadResult<TPayload>?> ReadJsonAsync<TPayload>(
        StoragePayloadKey key,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes a raw binary payload.
    /// </summary>
    Task<PayloadMetadata> WriteBinaryAsync(
        StoragePayloadKey key,
        Stream content,
        PayloadWriteOptions? options = null,
        StorageWriteCondition condition = default,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Opens a payload stream for reading.
    /// </summary>
    Task<PayloadStreamResult?> OpenReadAsync(
        StoragePayloadKey key,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads payload metadata without reading the payload body.
    /// </summary>
    Task<PayloadMetadata?> GetMetadataAsync(
        StoragePayloadKey key,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a payload.
    /// </summary>
    Task<bool> DeleteAsync(
        StoragePayloadKey key,
        StorageWriteCondition condition,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Provides queue-oriented work storage with claim, complete, and abandon semantics.
/// </summary>
/// <typeparam name="TWorkItem">The work-item payload type.</typeparam>
public interface IWorkStore<TWorkItem>
{
    /// <summary>
    /// Enqueues a work item.
    /// </summary>
    Task EnqueueAsync(
        WorkItem<TWorkItem> item,
        WorkEnqueueOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Claims the next visible work item.
    /// </summary>
    Task<ClaimedWorkItem<TWorkItem>?> ClaimAsync(
        WorkClaimOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Completes a previously claimed work item.
    /// </summary>
    Task CompleteAsync(
        WorkClaimToken claimToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Releases a claimed work item back to the queue.
    /// </summary>
    Task AbandonAsync(
        WorkClaimToken claimToken,
        WorkReleaseOptions? options = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Provides idempotency markers, leases, and checkpoints.
/// </summary>
public interface ICoordinationStore
{
    /// <summary>
    /// Attempts to create an idempotency marker.
    /// </summary>
    Task<bool> TryCreateIdempotencyMarkerAsync(
        StorageRecordKey key,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether an idempotency marker exists.
    /// </summary>
    Task<bool> IdempotencyMarkerExistsAsync(
        StorageRecordKey key,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to acquire a coordination lease.
    /// </summary>
    Task<CoordinationLease?> TryAcquireLeaseAsync(
        CoordinationLeaseRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Renews a previously acquired lease.
    /// </summary>
    Task<CoordinationLease> RenewLeaseAsync(
        CoordinationLease lease,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Releases a previously acquired lease.
    /// </summary>
    Task ReleaseLeaseAsync(
        CoordinationLease lease,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads a typed checkpoint.
    /// </summary>
    Task<StorageItem<TState>?> GetCheckpointAsync<TState>(
        StorageRecordKey key,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes a typed checkpoint.
    /// </summary>
    Task<StorageItem<TState>> WriteCheckpointAsync<TState>(
        StorageRecordKey key,
        TState state,
        StorageWriteCondition condition,
        CancellationToken cancellationToken = default);
}
