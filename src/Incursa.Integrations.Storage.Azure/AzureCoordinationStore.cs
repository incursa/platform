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

using Azure;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Incursa.Platform.Storage;
using Microsoft.Extensions.Logging;

namespace Incursa.Integrations.Storage.Azure;

internal sealed class AzureCoordinationStore : ICoordinationStore
{
    private const string IdempotencyKind = "marker";
    private const string CheckpointKind = "checkpoint";

    private readonly AzureStorageOptions options;
    private readonly AzureStorageNameResolver nameResolver;
    private readonly AzureStorageJsonSerializer serializer;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<AzureCoordinationStore> logger;
    private readonly TableClient tableClient;
    private readonly BlobContainerClient containerClient;
    private readonly SemaphoreSlim tableEnsureGate = new(1, 1);
    private readonly SemaphoreSlim containerEnsureGate = new(1, 1);
    private bool tableEnsured;
    private bool containerEnsured;

    public AzureCoordinationStore(
        AzureStorageClientFactory clientFactory,
        AzureStorageOptions options,
        AzureStorageNameResolver nameResolver,
        AzureStorageJsonSerializer serializer,
        TimeProvider timeProvider,
        ILogger<AzureCoordinationStore> logger)
    {
        ArgumentNullException.ThrowIfNull(clientFactory);
        this.options = options ?? throw new ArgumentNullException(nameof(options));
        this.nameResolver = nameResolver ?? throw new ArgumentNullException(nameof(nameResolver));
        this.serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        this.timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        tableClient = clientFactory.TableServiceClient.GetTableClient(options.CoordinationTableName);
        containerClient = clientFactory.BlobServiceClient.GetBlobContainerClient(options.CoordinationContainerName);
    }

    public async Task<bool> TryCreateIdempotencyMarkerAsync(
        StorageRecordKey key,
        CancellationToken cancellationToken = default)
    {
        await EnsureTableReadyAsync(cancellationToken).ConfigureAwait(false);

        TableEntity entity = new(nameResolver.GetCoordinationPartitionKey(IdempotencyKind, key), key.RowKey.Value)
        {
            [AzureStorageExceptionHelper.DataPropertyName] = "true",
        };

        try
        {
            await tableClient.AddEntityAsync(entity, cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (RequestFailedException exception) when (AzureStorageExceptionHelper.IsConflictOrPrecondition(exception))
        {
            return false;
        }
    }

    public async Task<bool> IdempotencyMarkerExistsAsync(
        StorageRecordKey key,
        CancellationToken cancellationToken = default)
    {
        await EnsureTableReadyAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            NullableResponse<TableEntity> response = await tableClient
                .GetEntityIfExistsAsync<TableEntity>(
                    nameResolver.GetCoordinationPartitionKey(IdempotencyKind, key),
                    key.RowKey.Value,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return response.HasValue;
        }
        catch (RequestFailedException exception) when (AzureStorageExceptionHelper.IsNotFound(exception))
        {
            return false;
        }
    }

    public async Task<CoordinationLease?> TryAcquireLeaseAsync(
        CoordinationLeaseRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Duration < TimeSpan.FromSeconds(15) || request.Duration > TimeSpan.FromSeconds(60))
        {
            throw new StorageOperationNotSupportedException(
                "Azure blob leases support finite lease durations from 15 seconds to 60 seconds.");
        }

        await EnsureContainerReadyAsync(cancellationToken).ConfigureAwait(false);

        BlobClient blobClient = containerClient.GetBlobClient(nameResolver.GetLeaseBlobName(request.Key));
        await EnsureLeaseBlobExistsAsync(blobClient, cancellationToken).ConfigureAwait(false);

        BlobLeaseClient leaseClient = blobClient.GetBlobLeaseClient();
        try
        {
            Response<BlobLease> response = await leaseClient
                .AcquireAsync(request.Duration, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            DateTimeOffset acquiredUtc = timeProvider.GetUtcNow();
            return new CoordinationLease(
                request.Key,
                request.Owner,
                new CoordinationLeaseToken(response.Value.LeaseId),
                acquiredUtc,
                acquiredUtc.Add(request.Duration),
                request.Duration);
        }
        catch (RequestFailedException exception) when (AzureStorageExceptionHelper.IsConflictOrPrecondition(exception))
        {
            return null;
        }
    }

    public async Task<CoordinationLease> RenewLeaseAsync(
        CoordinationLease lease,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(lease);

        await EnsureContainerReadyAsync(cancellationToken).ConfigureAwait(false);

        BlobLeaseClient leaseClient = containerClient
            .GetBlobClient(nameResolver.GetLeaseBlobName(lease.Key))
            .GetBlobLeaseClient(lease.Token.Value);

        try
        {
            await leaseClient.RenewAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (RequestFailedException exception) when (AzureStorageExceptionHelper.IsNotFound(exception) ||
                                                       AzureStorageExceptionHelper.IsConflictOrPrecondition(exception))
        {
            throw new StoragePreconditionFailedException("The coordination lease could not be renewed because it is no longer valid.", exception);
        }

        DateTimeOffset renewedUtc = timeProvider.GetUtcNow();
        return lease with
        {
            AcquiredUtc = renewedUtc,
            ExpiresUtc = renewedUtc.Add(lease.Duration),
        };
    }

    public async Task ReleaseLeaseAsync(
        CoordinationLease lease,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(lease);

        await EnsureContainerReadyAsync(cancellationToken).ConfigureAwait(false);
        BlobLeaseClient leaseClient = containerClient
            .GetBlobClient(nameResolver.GetLeaseBlobName(lease.Key))
            .GetBlobLeaseClient(lease.Token.Value);

        try
        {
            await leaseClient.ReleaseAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (RequestFailedException exception) when (AzureStorageExceptionHelper.IsNotFound(exception) ||
                                                       AzureStorageExceptionHelper.IsConflictOrPrecondition(exception))
        {
            throw new StoragePreconditionFailedException("The coordination lease could not be released because it is no longer valid.", exception);
        }
    }

    public async Task<StorageItem<TState>?> GetCheckpointAsync<TState>(
        StorageRecordKey key,
        CancellationToken cancellationToken = default)
    {
        await EnsureTableReadyAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            NullableResponse<TableEntity> response = await tableClient
                .GetEntityIfExistsAsync<TableEntity>(
                    nameResolver.GetCoordinationPartitionKey(CheckpointKind, key),
                    key.RowKey.Value,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (!response.HasValue)
            {
                return null;
            }

            return ToCheckpoint<TState>(key, response.Value!);
        }
        catch (RequestFailedException exception) when (AzureStorageExceptionHelper.IsNotFound(exception))
        {
            return null;
        }
    }

    public async Task<StorageItem<TState>> WriteCheckpointAsync<TState>(
        StorageRecordKey key,
        TState state,
        StorageWriteCondition condition,
        CancellationToken cancellationToken = default)
    {
        await EnsureTableReadyAsync(cancellationToken).ConfigureAwait(false);

        TableEntity entity = new(nameResolver.GetCoordinationPartitionKey(CheckpointKind, key), key.RowKey.Value)
        {
            [AzureStorageExceptionHelper.DataPropertyName] = serializer.SerializeToString(state),
        };

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
                    await tableClient.UpdateEntityAsync(entity, AzureStorageConditionMapper.RequireAzureETag(condition), TableUpdateMode.Replace, cancellationToken).ConfigureAwait(false);
                    break;
                default:
                    throw new StorageOperationNotSupportedException($"Unsupported checkpoint write condition kind '{condition.Kind}'.");
            }
        }
        catch (RequestFailedException exception) when (AzureStorageExceptionHelper.IsConflictOrPrecondition(exception))
        {
            throw new StoragePreconditionFailedException(
                $"The checkpoint write for '{key}' did not satisfy the requested optimistic concurrency condition.",
                exception);
        }

        StorageItem<TState>? stored = await GetCheckpointAsync<TState>(key, cancellationToken).ConfigureAwait(false);
        return stored ?? throw new StorageException($"The checkpoint '{key}' was written but could not be reloaded.");
    }

    private async Task EnsureTableReadyAsync(CancellationToken cancellationToken)
    {
        if (tableEnsured || !options.CreateResourcesIfMissing)
        {
            return;
        }

        await tableEnsureGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (tableEnsured)
            {
                return;
            }

            logger.LogDebug("Ensuring Azure Table '{TableName}' exists for coordination state.", tableClient.Name);
            await tableClient.CreateIfNotExistsAsync(cancellationToken).ConfigureAwait(false);
            tableEnsured = true;
        }
        finally
        {
            tableEnsureGate.Release();
        }
    }

    private async Task EnsureContainerReadyAsync(CancellationToken cancellationToken)
    {
        if (containerEnsured || !options.CreateResourcesIfMissing)
        {
            return;
        }

        await containerEnsureGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (containerEnsured)
            {
                return;
            }

            logger.LogDebug("Ensuring Azure Blob container '{ContainerName}' exists for coordination leases.", containerClient.Name);
            await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            containerEnsured = true;
        }
        finally
        {
            containerEnsureGate.Release();
        }
    }

    private async Task EnsureLeaseBlobExistsAsync(BlobClient blobClient, CancellationToken cancellationToken)
    {
        try
        {
            await blobClient.UploadAsync(new BinaryData(Array.Empty<byte>()), overwrite: false, cancellationToken).ConfigureAwait(false);
        }
        catch (RequestFailedException exception) when (AzureStorageExceptionHelper.IsConflictOrPrecondition(exception))
        {
            // The lease blob already exists. This is expected after first acquisition.
        }
    }

    private StorageItem<TState> ToCheckpoint<TState>(StorageRecordKey key, TableEntity entity)
    {
        string payload = entity.GetString(AzureStorageExceptionHelper.DataPropertyName)
            ?? throw new StorageException($"The checkpoint '{key}' does not contain the expected data payload.");

        TState? state = serializer.Deserialize<TState>(payload);
        return new StorageItem<TState>(key, state!, new StorageETag(entity.ETag.ToString()), entity.Timestamp);
    }
}
