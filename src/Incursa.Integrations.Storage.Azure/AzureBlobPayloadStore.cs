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

using System.Security.Cryptography;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Incursa.Platform.Storage;
using Microsoft.Extensions.Logging;

namespace Incursa.Integrations.Storage.Azure;

internal sealed class AzureBlobPayloadStore : IPayloadStore
{
    private readonly AzureStorageOptions options;
    private readonly AzureStorageNameResolver nameResolver;
    private readonly AzureStorageJsonSerializer serializer;
    private readonly ILogger<AzureBlobPayloadStore> logger;
    private readonly BlobContainerClient containerClient;
    private readonly SemaphoreSlim ensureGate = new(1, 1);
    private bool ensured;

    public AzureBlobPayloadStore(
        AzureStorageClientFactory clientFactory,
        AzureStorageOptions options,
        AzureStorageNameResolver nameResolver,
        AzureStorageJsonSerializer serializer,
        ILogger<AzureBlobPayloadStore> logger)
    {
        ArgumentNullException.ThrowIfNull(clientFactory);
        this.options = options ?? throw new ArgumentNullException(nameof(options));
        this.nameResolver = nameResolver ?? throw new ArgumentNullException(nameof(nameResolver));
        this.serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        containerClient = clientFactory.BlobServiceClient.GetBlobContainerClient(options.PayloadContainerName);
    }

    public async Task<PayloadMetadata> WriteJsonAsync<TPayload>(
        StoragePayloadKey key,
        TPayload value,
        PayloadWriteOptions? writeOptions = null,
        StorageWriteCondition condition = default,
        CancellationToken cancellationToken = default)
    {
        byte[] payload = serializer.SerializeToBytes(value);
        using MemoryStream content = new(payload, writable: false);

        PayloadWriteOptions effectiveOptions = MergeWriteOptions(writeOptions, "application/json; charset=utf-8", ComputeChecksum(payload));
        return await WriteBinaryCoreAsync(key, content, effectiveOptions, condition, cancellationToken).ConfigureAwait(false);
    }

    public async Task<PayloadReadResult<TPayload>?> ReadJsonAsync<TPayload>(
        StoragePayloadKey key,
        CancellationToken cancellationToken = default)
    {
        PayloadStreamResult? opened = await OpenReadAsync(key, cancellationToken).ConfigureAwait(false);
        if (opened is null)
        {
            return null;
        }

        try
        {
            TPayload? value = await serializer.DeserializeAsync<TPayload>(opened.Content, cancellationToken).ConfigureAwait(false);
            return new PayloadReadResult<TPayload>(opened.Metadata, value!);
        }
        finally
        {
            await opened.DisposeAsync().ConfigureAwait(false);
        }
    }

    public async Task<PayloadMetadata> WriteBinaryAsync(
        StoragePayloadKey key,
        Stream content,
        PayloadWriteOptions? writeOptions = null,
        StorageWriteCondition condition = default,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        using MemoryStream buffered = new();
        await content.CopyToAsync(buffered, cancellationToken).ConfigureAwait(false);
        buffered.Position = 0;

        string checksum = writeOptions?.Checksum ?? ComputeChecksum(buffered.ToArray());
        PayloadWriteOptions effectiveOptions = MergeWriteOptions(writeOptions, writeOptions?.ContentType ?? "application/octet-stream", checksum);
        return await WriteBinaryCoreAsync(key, buffered, effectiveOptions, condition, cancellationToken).ConfigureAwait(false);
    }

    public async Task<PayloadStreamResult?> OpenReadAsync(
        StoragePayloadKey key,
        CancellationToken cancellationToken = default)
    {
        await EnsureContainerReadyAsync(cancellationToken).ConfigureAwait(false);

        BlobClient blobClient = containerClient.GetBlobClient(nameResolver.GetPayloadBlobName(key));
        try
        {
            Response<BlobDownloadStreamingResult> response = await blobClient.DownloadStreamingAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            PayloadMetadata metadata = AzurePayloadMetadataAdapter.Build(key, response.Value.Details);
            return new PayloadStreamResult(metadata, response.Value.Content);
        }
        catch (RequestFailedException exception) when (AzureStorageExceptionHelper.IsNotFound(exception))
        {
            return null;
        }
    }

    public async Task<PayloadMetadata?> GetMetadataAsync(
        StoragePayloadKey key,
        CancellationToken cancellationToken = default)
    {
        await EnsureContainerReadyAsync(cancellationToken).ConfigureAwait(false);

        BlobClient blobClient = containerClient.GetBlobClient(nameResolver.GetPayloadBlobName(key));
        try
        {
            Response<BlobProperties> response = await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            return AzurePayloadMetadataAdapter.Build(key, response.Value);
        }
        catch (RequestFailedException exception) when (AzureStorageExceptionHelper.IsNotFound(exception))
        {
            return null;
        }
    }

    public async Task<bool> DeleteAsync(
        StoragePayloadKey key,
        StorageWriteCondition condition,
        CancellationToken cancellationToken = default)
    {
        await EnsureContainerReadyAsync(cancellationToken).ConfigureAwait(false);

        if (condition.Kind == StorageWriteConditionKind.IfNotExists)
        {
            throw new StorageOperationNotSupportedException("Payload delete operations do not support IfNotExists preconditions.");
        }

        BlobClient blobClient = containerClient.GetBlobClient(nameResolver.GetPayloadBlobName(key));
        if (condition.Kind == StorageWriteConditionKind.Unconditional)
        {
            Response<bool> response = await blobClient
                .DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return response.Value;
        }

        try
        {
            await blobClient.DeleteAsync(
                    DeleteSnapshotsOption.IncludeSnapshots,
                    new BlobRequestConditions { IfMatch = AzureStorageConditionMapper.RequireAzureETag(condition) },
                    cancellationToken)
                .ConfigureAwait(false);

            return true;
        }
        catch (RequestFailedException exception) when (AzureStorageExceptionHelper.IsNotFound(exception) ||
                                                       AzureStorageExceptionHelper.IsConflictOrPrecondition(exception))
        {
            throw new StoragePreconditionFailedException(
                $"The payload delete operation for '{key}' did not satisfy the requested optimistic concurrency condition.",
                exception);
        }
    }

    private async Task<PayloadMetadata> WriteBinaryCoreAsync(
        StoragePayloadKey key,
        Stream content,
        PayloadWriteOptions writeOptions,
        StorageWriteCondition condition,
        CancellationToken cancellationToken)
    {
        await EnsureContainerReadyAsync(cancellationToken).ConfigureAwait(false);

        BlobClient blobClient = containerClient.GetBlobClient(nameResolver.GetPayloadBlobName(key));
        BlobUploadOptions uploadOptions = new()
        {
            Conditions = BuildRequestConditions(condition),
            HttpHeaders = new BlobHttpHeaders
            {
                ContentType = writeOptions.ContentType,
            },
            Metadata = AzurePayloadMetadataAdapter.BuildMetadataDictionary(writeOptions),
        };

        content.Position = 0;
        try
        {
            await blobClient.UploadAsync(content, uploadOptions, cancellationToken).ConfigureAwait(false);
        }
        catch (RequestFailedException exception) when (AzureStorageExceptionHelper.IsConflictOrPrecondition(exception))
        {
            throw new StoragePreconditionFailedException(
                $"The payload write operation for '{key}' did not satisfy the requested optimistic concurrency condition.",
                exception);
        }

        PayloadMetadata? metadata = await GetMetadataAsync(key, cancellationToken).ConfigureAwait(false);
        return metadata ?? throw new StorageException($"The payload '{key}' was written but its metadata could not be reloaded.");
    }

    private async Task EnsureContainerReadyAsync(CancellationToken cancellationToken)
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

            logger.LogDebug("Ensuring Azure Blob container '{ContainerName}' exists.", containerClient.Name);
            await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            ensured = true;
        }
        finally
        {
            ensureGate.Release();
        }
    }

    private static BlobRequestConditions? BuildRequestConditions(StorageWriteCondition condition) =>
        AzureStorageConditionMapper.ToBlobRequestConditions(condition);

    private static string ComputeChecksum(byte[] payload)
    {
        byte[] hash = SHA256.HashData(payload);
        return Convert.ToHexString(hash);
    }

    private static PayloadWriteOptions MergeWriteOptions(PayloadWriteOptions? writeOptions, string contentType, string checksum)
    {
        return new PayloadWriteOptions
        {
            ContentType = contentType,
            SchemaVersion = writeOptions?.SchemaVersion,
            Checksum = checksum,
            Metadata = writeOptions?.Metadata,
        };
    }

}
