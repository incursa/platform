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

using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Incursa.Platform.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace Incursa.Integrations.Storage.Azure.Tests;

[Trait("Category", "Integration")]
[Trait("RequiresEmulator", "Azurite")]
public sealed class AzureQueueWorkStoreIntegrationTests
{
    [Fact]
    public async Task EnqueueClaimAbandonAndComplete_RoundTripSmallPayload()
    {
        AzuriteTestEnvironment environment = await AzuriteTestEnvironment.GetBlobAndQueueAsync().ConfigureAwait(false);
        AzureStorageOptions options = AzureStorageTestOptions.CreateIntegrationOptions(environment.ConnectionString, nameof(EnqueueClaimAbandonAndComplete_RoundTripSmallPayload));
        FakeTimeProvider timeProvider = new(DateTimeOffset.Parse("2026-03-08T12:00:00Z", null, System.Globalization.DateTimeStyles.RoundtripKind));
        using ServiceProvider provider = AzureStorageTestFactory.BuildServiceProvider(options, timeProvider);
        IWorkStore<SampleWorkItem> store = provider.GetRequiredService<IWorkStore<SampleWorkItem>>();

        WorkItem<SampleWorkItem> item = new(
            "work-1",
            new SampleWorkItem("compile", 1),
            correlationId: "corr-1",
            idempotencyKey: "idem-1",
            schemaVersion: "v1",
            metadata: new Dictionary<string, string>(StringComparer.Ordinal) { ["source"] = "tests" });

        await store.EnqueueAsync(item, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(false);

        ClaimedWorkItem<SampleWorkItem>? claimed = await store
            .ClaimAsync(new WorkClaimOptions(TimeSpan.FromSeconds(30)), TestContext.Current.CancellationToken)
            .ConfigureAwait(false);

        claimed.ShouldNotBeNull();
        claimed!.Item.Id.ShouldBe("work-1");
        claimed.Item.Payload.ShouldBe(new SampleWorkItem("compile", 1));
        claimed.Item.CorrelationId.ShouldBe("corr-1");
        claimed.Item.IdempotencyKey.ShouldBe("idem-1");
        claimed.Item.Metadata["source"].ShouldBe("tests");
        claimed.ClaimedUntilUtc.ShouldBe(timeProvider.GetUtcNow().AddSeconds(30));
        claimed.DeliveryCount.ShouldBe(1);

        await store
            .AbandonAsync(claimed.ClaimToken, new WorkReleaseOptions { VisibilityDelay = TimeSpan.Zero }, TestContext.Current.CancellationToken)
            .ConfigureAwait(false);

        ClaimedWorkItem<SampleWorkItem> reclaimed = await ClaimEventuallyAsync(store).ConfigureAwait(false);
        reclaimed.DeliveryCount.ShouldBeGreaterThan(1);

        await store.CompleteAsync(reclaimed.ClaimToken, TestContext.Current.CancellationToken).ConfigureAwait(false);
        (await store.ClaimAsync(new WorkClaimOptions(TimeSpan.FromSeconds(30)), TestContext.Current.CancellationToken).ConfigureAwait(false)).ShouldBeNull();
    }

    [Fact]
    public async Task EnqueueAsync_UsesOverflowBlobForLargePayloads_AndCompleteDeletesIt()
    {
        AzuriteTestEnvironment environment = await AzuriteTestEnvironment.GetBlobAndQueueAsync().ConfigureAwait(false);
        AzureStorageOptions options = AzureStorageTestOptions.CreateIntegrationOptions(
            environment.ConnectionString,
            nameof(EnqueueAsync_UsesOverflowBlobForLargePayloads_AndCompleteDeletesIt),
            configure: storageOptions => storageOptions.WorkMessageInlineThresholdBytes = 128);
        using ServiceProvider provider = AzureStorageTestFactory.BuildServiceProvider(options);
        IWorkStore<SampleWorkItem> store = provider.GetRequiredService<IWorkStore<SampleWorkItem>>();
        BlobContainerClient payloadContainer = new BlobServiceClient(environment.ConnectionString).GetBlobContainerClient(options.WorkPayloadContainerName);

        WorkItem<SampleWorkItem> item = new(
            "work-2",
            new SampleWorkItem(new string('x', 4000), 4));

        await store.EnqueueAsync(item, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(false);
        IReadOnlyList<string> blobNamesBeforeClaim = await ListBlobNamesAsync(payloadContainer).ConfigureAwait(false);

        ClaimedWorkItem<SampleWorkItem>? claimed = await store
            .ClaimAsync(new WorkClaimOptions(TimeSpan.FromSeconds(30)), TestContext.Current.CancellationToken)
            .ConfigureAwait(false);

        claimed.ShouldNotBeNull();
        claimed!.Item.Payload.ShouldBe(item.Payload);
        blobNamesBeforeClaim.Count.ShouldBe(1);

        await store.CompleteAsync(claimed.ClaimToken, TestContext.Current.CancellationToken).ConfigureAwait(false);
        (await ListBlobNamesAsync(payloadContainer).ConfigureAwait(false)).ShouldBeEmpty();
    }

    private static async Task<ClaimedWorkItem<SampleWorkItem>> ClaimEventuallyAsync(IWorkStore<SampleWorkItem> store)
    {
        DateTimeOffset timeoutAt = DateTimeOffset.UtcNow.AddSeconds(10);
        while (DateTimeOffset.UtcNow < timeoutAt)
        {
            ClaimedWorkItem<SampleWorkItem>? claimed = await store
                .ClaimAsync(new WorkClaimOptions(TimeSpan.FromSeconds(30)), TestContext.Current.CancellationToken)
                .ConfigureAwait(false);

            if (claimed is not null)
            {
                return claimed;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(200), TestContext.Current.CancellationToken).ConfigureAwait(false);
        }

        throw new TimeoutException("The abandoned work item did not become visible again before the timeout.");
    }

    private static async Task<IReadOnlyList<string>> ListBlobNamesAsync(BlobContainerClient containerClient)
    {
        List<string> blobNames = new();
        await foreach (BlobItem blob in containerClient.GetBlobsAsync(cancellationToken: TestContext.Current.CancellationToken))
        {
            blobNames.Add(blob.Name);
        }

        return blobNames;
    }
}
