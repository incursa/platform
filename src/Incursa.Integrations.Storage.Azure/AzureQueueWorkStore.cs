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

using System.Text;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Incursa.Platform.Storage;
using Microsoft.Extensions.Logging;

namespace Incursa.Integrations.Storage.Azure;

internal sealed class AzureQueueWorkStore<TWorkItem> : IWorkStore<TWorkItem>
{
    private readonly AzureStorageOptions options;
    private readonly AzureStorageNameResolver nameResolver;
    private readonly AzureStorageJsonSerializer serializer;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<AzureQueueWorkStore<TWorkItem>> logger;
    private readonly QueueClient queueClient;
    private readonly BlobContainerClient payloadContainerClient;
    private readonly SemaphoreSlim queueEnsureGate = new(1, 1);
    private readonly SemaphoreSlim payloadEnsureGate = new(1, 1);
    private bool queueEnsured;
    private bool payloadContainerEnsured;

    public AzureQueueWorkStore(
        AzureStorageClientFactory clientFactory,
        AzureStorageOptions options,
        AzureStorageNameResolver nameResolver,
        AzureStorageJsonSerializer serializer,
        TimeProvider timeProvider,
        ILogger<AzureQueueWorkStore<TWorkItem>> logger)
    {
        ArgumentNullException.ThrowIfNull(clientFactory);
        this.options = options ?? throw new ArgumentNullException(nameof(options));
        this.nameResolver = nameResolver ?? throw new ArgumentNullException(nameof(nameResolver));
        this.serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        this.timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        queueClient = clientFactory.QueueServiceClient.GetQueueClient(nameResolver.GetWorkQueueName(typeof(TWorkItem)));
        payloadContainerClient = clientFactory.BlobServiceClient.GetBlobContainerClient(options.WorkPayloadContainerName);
    }

    public async Task EnqueueAsync(
        WorkItem<TWorkItem> item,
        WorkEnqueueOptions? enqueueOptions = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);

        await EnsureQueueReadyAsync(cancellationToken).ConfigureAwait(false);

        byte[] payloadBytes = serializer.SerializeToBytes(item.Payload);
        AzureQueuedWorkEnvelope envelope = AzureQueuedWorkEnvelope.CreateInline(
            item,
            Convert.ToBase64String(payloadBytes),
            timeProvider.GetUtcNow());

        string queueBody = serializer.SerializeToString(envelope);
        if (Encoding.UTF8.GetByteCount(queueBody) > options.WorkMessageInlineThresholdBytes)
        {
            await EnsurePayloadContainerReadyAsync(cancellationToken).ConfigureAwait(false);

            string blobName = nameResolver.GetWorkPayloadBlobName(typeof(TWorkItem), item.Id);
            await payloadContainerClient.GetBlobClient(blobName)
                .UploadAsync(new BinaryData(payloadBytes), overwrite: true, cancellationToken)
                .ConfigureAwait(false);

            envelope = AzureQueuedWorkEnvelope.CreateReference(item, blobName, timeProvider.GetUtcNow());
            queueBody = serializer.SerializeToString(envelope);
        }

        string encodedBody = Convert.ToBase64String(Encoding.UTF8.GetBytes(queueBody));
        await queueClient.SendMessageAsync(
                encodedBody,
                enqueueOptions?.InitialVisibilityDelay,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<ClaimedWorkItem<TWorkItem>?> ClaimAsync(
        WorkClaimOptions claimOptions,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(claimOptions);

        await EnsureQueueReadyAsync(cancellationToken).ConfigureAwait(false);

        Response<QueueMessage[]> response = await queueClient.ReceiveMessagesAsync(
                maxMessages: 1,
                visibilityTimeout: claimOptions.VisibilityTimeout,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        QueueMessage? message = response.Value.SingleOrDefault();
        if (message is null)
        {
            return null;
        }

        AzureQueuedWorkEnvelope envelope = DecodeEnvelope(message.MessageText);
        TWorkItem payload = await ReadPayloadAsync(envelope, cancellationToken).ConfigureAwait(false);

        WorkItem<TWorkItem> item = new(
            envelope.Id,
            payload,
            envelope.CorrelationId,
            envelope.IdempotencyKey,
            envelope.SchemaVersion,
            envelope.Metadata);

        AzureWorkClaimTokenModel token = new(message.MessageId, message.PopReceipt, message.MessageText, envelope.PayloadReference);
        string encodedToken = Convert.ToBase64String(serializer.SerializeToBytes(token));

        return new ClaimedWorkItem<TWorkItem>(
            item,
            new WorkClaimToken(encodedToken),
            timeProvider.GetUtcNow().Add(claimOptions.VisibilityTimeout),
            checked((int)message.DequeueCount));
    }

    public async Task CompleteAsync(
        WorkClaimToken claimToken,
        CancellationToken cancellationToken = default)
    {
        AzureWorkClaimTokenModel token = DecodeToken(claimToken);
        await EnsureQueueReadyAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            await queueClient.DeleteMessageAsync(token.MessageId, token.PopReceipt, cancellationToken).ConfigureAwait(false);
        }
        catch (RequestFailedException exception) when (AzureStorageExceptionHelper.IsNotFound(exception) ||
                                                       AzureStorageExceptionHelper.IsConflictOrPrecondition(exception))
        {
            throw new StoragePreconditionFailedException(
                "The work item could not be completed because the queue claim is no longer valid.",
                exception);
        }

        if (!string.IsNullOrWhiteSpace(token.PayloadReference))
        {
            await EnsurePayloadContainerReadyAsync(cancellationToken).ConfigureAwait(false);
            await payloadContainerClient.GetBlobClient(token.PayloadReference)
                .DeleteIfExistsAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
    }

    public async Task AbandonAsync(
        WorkClaimToken claimToken,
        WorkReleaseOptions? releaseOptions = null,
        CancellationToken cancellationToken = default)
    {
        AzureWorkClaimTokenModel token = DecodeToken(claimToken);
        await EnsureQueueReadyAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            await queueClient.UpdateMessageAsync(
                    token.MessageId,
                    token.PopReceipt,
                    token.MessageText,
                    releaseOptions?.VisibilityDelay ?? TimeSpan.Zero,
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (RequestFailedException exception) when (AzureStorageExceptionHelper.IsNotFound(exception) ||
                                                       AzureStorageExceptionHelper.IsConflictOrPrecondition(exception))
        {
            throw new StoragePreconditionFailedException(
                "The work item could not be released because the queue claim is no longer valid.",
                exception);
        }
    }

    private async Task<TWorkItem> ReadPayloadAsync(AzureQueuedWorkEnvelope envelope, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(envelope.PayloadInline))
        {
            return serializer.Deserialize<TWorkItem>(Convert.FromBase64String(envelope.PayloadInline))!;
        }

        if (string.IsNullOrWhiteSpace(envelope.PayloadReference))
        {
            throw new StorageException("The claimed work item does not contain an inline payload or a payload reference.");
        }

        await EnsurePayloadContainerReadyAsync(cancellationToken).ConfigureAwait(false);
        Response<BlobDownloadResult> response = await payloadContainerClient
            .GetBlobClient(envelope.PayloadReference)
            .DownloadContentAsync(cancellationToken)
            .ConfigureAwait(false);

        return serializer.Deserialize<TWorkItem>(response.Value.Content.ToArray())!;
    }

    private AzureQueuedWorkEnvelope DecodeEnvelope(string messageText)
    {
        string decoded = Encoding.UTF8.GetString(Convert.FromBase64String(messageText));
        return serializer.Deserialize<AzureQueuedWorkEnvelope>(decoded)
            ?? throw new StorageException("The queue message did not contain a valid work envelope.");
    }

    private AzureWorkClaimTokenModel DecodeToken(WorkClaimToken claimToken)
    {
        byte[] tokenBytes = Convert.FromBase64String(claimToken.Value);
        return serializer.Deserialize<AzureWorkClaimTokenModel>(tokenBytes)
            ?? throw new StorageException("The supplied work-claim token is invalid.");
    }

    private async Task EnsureQueueReadyAsync(CancellationToken cancellationToken)
    {
        if (queueEnsured || !options.CreateResourcesIfMissing)
        {
            return;
        }

        await queueEnsureGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (queueEnsured)
            {
                return;
            }

            logger.LogDebug("Ensuring Azure Queue '{QueueName}' exists.", queueClient.Name);
            await queueClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            queueEnsured = true;
        }
        finally
        {
            queueEnsureGate.Release();
        }
    }

    private async Task EnsurePayloadContainerReadyAsync(CancellationToken cancellationToken)
    {
        if (payloadContainerEnsured || !options.CreateResourcesIfMissing)
        {
            return;
        }

        await payloadEnsureGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (payloadContainerEnsured)
            {
                return;
            }

            logger.LogDebug("Ensuring Azure Blob container '{ContainerName}' exists for work payload overflow.", payloadContainerClient.Name);
            await payloadContainerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            payloadContainerEnsured = true;
        }
        finally
        {
            payloadEnsureGate.Release();
        }
    }

    internal sealed record AzureQueuedWorkEnvelope(
        string Id,
        string? CorrelationId,
        string? IdempotencyKey,
        string? SchemaVersion,
        IReadOnlyDictionary<string, string> Metadata,
        DateTimeOffset EnqueuedUtc,
        string? PayloadInline,
        string? PayloadReference)
    {
        internal static AzureQueuedWorkEnvelope CreateInline<TPayload>(
            WorkItem<TPayload> item,
            string payloadInline,
            DateTimeOffset enqueuedUtc) =>
            new(
                item.Id,
                item.CorrelationId,
                item.IdempotencyKey,
                item.SchemaVersion,
                item.Metadata,
                enqueuedUtc,
                payloadInline,
                null);

        internal static AzureQueuedWorkEnvelope CreateReference<TPayload>(
            WorkItem<TPayload> item,
            string payloadReference,
            DateTimeOffset enqueuedUtc) =>
            new(
                item.Id,
                item.CorrelationId,
                item.IdempotencyKey,
                item.SchemaVersion,
                item.Metadata,
                enqueuedUtc,
                null,
                payloadReference);
    }

    internal sealed record AzureWorkClaimTokenModel(
        string MessageId,
        string PopReceipt,
        string MessageText,
        string? PayloadReference);
}
