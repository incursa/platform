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

using Incursa.Platform.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Incursa.Integrations.Storage.Azure.Tests;

[Trait("Category", "Integration")]
[Trait("RequiresEmulator", "AzuriteTables")]
public sealed class AzureTableStoreIntegrationTests
{
    [Fact]
    public async Task WriteAsync_AndGetAsync_RoundTripExactKeyAndEnforceOptimisticConcurrency()
    {
        AzuriteTestEnvironment environment = await AzuriteTestEnvironment.GetTableAsync().ConfigureAwait(false);
        AzureStorageOptions options = AzureStorageTestOptions.CreateIntegrationOptions(environment.ConnectionString, nameof(WriteAsync_AndGetAsync_RoundTripExactKeyAndEnforceOptimisticConcurrency));
        using ServiceProvider provider = AzureStorageTestFactory.BuildServiceProvider(options);
        IRecordStore<SampleRecord> store = provider.GetRequiredService<IRecordStore<SampleRecord>>();
        StorageRecordKey key = new(new StoragePartitionKey("customers"), new StorageRowKey("001"));

        StorageItem<SampleRecord> first = await store
            .WriteAsync(key, new SampleRecord("001", "Ada"), StorageWriteMode.Upsert, StorageWriteCondition.IfNotExists(), TestContext.Current.CancellationToken)
            .ConfigureAwait(false);

        StorageItem<SampleRecord>? loaded = await store.GetAsync(key, TestContext.Current.CancellationToken).ConfigureAwait(false);
        loaded.ShouldNotBeNull();
        loaded!.Value.ShouldBe(new SampleRecord("001", "Ada"));

        StorageItem<SampleRecord> updated = await store
            .WriteAsync(
                key,
                new SampleRecord("001", "Grace"),
                StorageWriteMode.Put,
                StorageWriteCondition.IfMatch(first.ETag),
                TestContext.Current.CancellationToken)
            .ConfigureAwait(false);

        await Should.ThrowAsync<StoragePreconditionFailedException>(() =>
            store.WriteAsync(
                key,
                new SampleRecord("001", "Stale"),
                StorageWriteMode.Put,
                StorageWriteCondition.IfMatch(first.ETag),
                TestContext.Current.CancellationToken));

        bool deleted = await store
            .DeleteAsync(key, StorageWriteCondition.IfMatch(updated.ETag), TestContext.Current.CancellationToken)
            .ConfigureAwait(false);

        deleted.ShouldBeTrue();
        (await store.GetAsync(key, TestContext.Current.CancellationToken).ConfigureAwait(false)).ShouldBeNull();
    }

    [Fact]
    public async Task ExecuteBatchAsync_WritesSamePartitionOperationsAtomically()
    {
        AzuriteTestEnvironment environment = await AzuriteTestEnvironment.GetTableAsync().ConfigureAwait(false);
        AzureStorageOptions options = AzureStorageTestOptions.CreateIntegrationOptions(environment.ConnectionString, nameof(ExecuteBatchAsync_WritesSamePartitionOperationsAtomically));
        using ServiceProvider provider = AzureStorageTestFactory.BuildServiceProvider(options);
        IRecordStore<SampleRecord> store = provider.GetRequiredService<IRecordStore<SampleRecord>>();
        StoragePartitionKey partitionKey = new("customers");
        StorageBatch<SampleRecord> batch = new(
            new[]
            {
                StorageBatchOperation<SampleRecord>.Put(
                    new StorageRecordKey(partitionKey, new StorageRowKey("001")),
                    new SampleRecord("001", "Ada"),
                    StorageWriteCondition.IfNotExists()),
                StorageBatchOperation<SampleRecord>.Put(
                    new StorageRecordKey(partitionKey, new StorageRowKey("002")),
                    new SampleRecord("002", "Grace"),
                    StorageWriteCondition.IfNotExists()),
            });

        await store.ExecuteBatchAsync(batch, TestContext.Current.CancellationToken).ConfigureAwait(false);

        List<StorageItem<SampleRecord>> items = new();
        await foreach (StorageItem<SampleRecord> item in store.QueryPartitionAsync(partitionKey, StoragePartitionQuery.All(), TestContext.Current.CancellationToken))
        {
            items.Add(item);
        }

        items.Count.ShouldBe(2);
        items.Select(item => item.Value).OrderBy(record => record.Id, StringComparer.Ordinal).ShouldBe(
            new[]
            {
                new SampleRecord("001", "Ada"),
                new SampleRecord("002", "Grace"),
            });
    }
}
