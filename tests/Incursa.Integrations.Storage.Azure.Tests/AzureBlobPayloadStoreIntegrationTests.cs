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
using Incursa.Platform.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Incursa.Integrations.Storage.Azure.Tests;

[Trait("Category", "Integration")]
[Trait("RequiresEmulator", "Azurite")]
public sealed class AzureBlobPayloadStoreIntegrationTests
{
    [Fact]
    public async Task WriteJsonAsync_StoresMetadataAndRoundTripsTypedPayload()
    {
        AzuriteTestEnvironment environment = await AzuriteTestEnvironment.GetBlobAndQueueAsync().ConfigureAwait(false);
        AzureStorageOptions options = AzureStorageTestOptions.CreateIntegrationOptions(environment.ConnectionString, nameof(WriteJsonAsync_StoresMetadataAndRoundTripsTypedPayload));
        using ServiceProvider provider = AzureStorageTestFactory.BuildServiceProvider(options);
        IPayloadStore store = provider.GetRequiredService<IPayloadStore>();
        StoragePayloadKey key = new("payloads", "sample.json");

        PayloadMetadata written = await store.WriteJsonAsync(
            key,
            new SamplePayload("hello", 3),
            new PayloadWriteOptions
            {
                SchemaVersion = "v1",
                Metadata = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["Correlation-Id"] = "corr-1",
                },
            },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(false);

        PayloadMetadata? metadata = await store.GetMetadataAsync(key, TestContext.Current.CancellationToken).ConfigureAwait(false);
        PayloadReadResult<SamplePayload>? read = await store.ReadJsonAsync<SamplePayload>(key, TestContext.Current.CancellationToken).ConfigureAwait(false);

        metadata.ShouldNotBeNull();
        read.ShouldNotBeNull();
        written.ContentType.ShouldBe("application/json; charset=utf-8");
        metadata!.SchemaVersion.ShouldBe("v1");
        metadata.Checksum.ShouldNotBeNullOrWhiteSpace();
        metadata.Metadata["correlationid"].ShouldBe("corr-1");
        read!.Value.ShouldBe(new SamplePayload("hello", 3));
        read.Metadata.ETag.ShouldBe(metadata.ETag);
    }

    [Fact]
    public async Task WriteBinaryAsync_EnforcesOptimisticConcurrencyOnUpdateAndDelete()
    {
        AzuriteTestEnvironment environment = await AzuriteTestEnvironment.GetBlobAndQueueAsync().ConfigureAwait(false);
        AzureStorageOptions options = AzureStorageTestOptions.CreateIntegrationOptions(environment.ConnectionString, nameof(WriteBinaryAsync_EnforcesOptimisticConcurrencyOnUpdateAndDelete));
        using ServiceProvider provider = AzureStorageTestFactory.BuildServiceProvider(options);
        IPayloadStore store = provider.GetRequiredService<IPayloadStore>();
        StoragePayloadKey key = new("payloads", "sample.bin");

        using MemoryStream firstContent = new(Encoding.UTF8.GetBytes("alpha"));
        PayloadMetadata first = await store
            .WriteBinaryAsync(key, firstContent, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(false);

        using MemoryStream secondContent = new(Encoding.UTF8.GetBytes("bravo"));
        PayloadMetadata second = await store
            .WriteBinaryAsync(
                key,
                secondContent,
                condition: StorageWriteCondition.IfMatch(first.ETag),
                cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(false);

        await Should.ThrowAsync<StoragePreconditionFailedException>(() =>
            WritePayloadAsync(store, key, "charlie", StorageWriteCondition.IfMatch(first.ETag)));

        await Should.ThrowAsync<StoragePreconditionFailedException>(() =>
            store.DeleteAsync(
                key,
                StorageWriteCondition.IfMatch(first.ETag),
                TestContext.Current.CancellationToken));

        bool deleted = await store
            .DeleteAsync(key, StorageWriteCondition.IfMatch(second.ETag), TestContext.Current.CancellationToken)
            .ConfigureAwait(false);

        deleted.ShouldBeTrue();
        (await store.GetMetadataAsync(key, TestContext.Current.CancellationToken).ConfigureAwait(false)).ShouldBeNull();
    }

    private static async Task<PayloadMetadata> WritePayloadAsync(
        IPayloadStore store,
        StoragePayloadKey key,
        string content,
        StorageWriteCondition condition)
    {
        using MemoryStream stream = new(Encoding.UTF8.GetBytes(content));
        return await store
            .WriteBinaryAsync(key, stream, condition: condition, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(false);
    }
}
