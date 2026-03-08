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

namespace Incursa.Integrations.Storage.Azure.Tests;

[Trait("Category", "Unit")]
public sealed class AzureStorageContractBehaviorTests
{
    [Fact]
    public async Task ExecuteBatchAsync_RejectsEventuallyConsistentIntent()
    {
        AzureTableRecordStore<SampleRecord> store = AzureStorageTestFactory.CreateRecordStore<SampleRecord>();
        StorageBatch<SampleRecord> batch = new(
            new[]
            {
                StorageBatchOperation<SampleRecord>.Put(
                    new StorageRecordKey(new StoragePartitionKey("customers"), new StorageRowKey("001")),
                    new SampleRecord("001", "Ada"),
                    StorageWriteCondition.IfNotExists()),
            },
            StorageConsistencyMode.CrossPartitionEventuallyConsistent);

        StorageOperationNotSupportedException exception = await Should.ThrowAsync<StorageOperationNotSupportedException>(() =>
            store.ExecuteBatchAsync(batch, TestContext.Current.CancellationToken));

        exception.Message.ShouldContain("single-partition atomic intent");
    }

    [Fact]
    public async Task DeleteAsync_RejectsIfNotExistsPrecondition()
    {
        AzureTableRecordStore<SampleRecord> store = AzureStorageTestFactory.CreateRecordStore<SampleRecord>();
        StorageRecordKey key = new(new StoragePartitionKey("customers"), new StorageRowKey("001"));

        StorageOperationNotSupportedException exception = await Should.ThrowAsync<StorageOperationNotSupportedException>(() =>
            store.DeleteAsync(key, StorageWriteCondition.IfNotExists(), TestContext.Current.CancellationToken));

        exception.Message.ShouldContain("IfNotExists");
    }

    [Fact]
    public async Task TryAcquireLeaseAsync_RejectsDurationsOutsideBlobLeaseRange()
    {
        AzureCoordinationStore store = AzureStorageTestFactory.CreateCoordinationStore();
        CoordinationLeaseRequest request = new(
            new StorageRecordKey(new StoragePartitionKey("locks"), new StorageRowKey("job-1")),
            owner: "tests",
            duration: TimeSpan.FromSeconds(10));

        StorageOperationNotSupportedException exception = await Should.ThrowAsync<StorageOperationNotSupportedException>(() =>
            store.TryAcquireLeaseAsync(request, TestContext.Current.CancellationToken));

        exception.Message.ShouldContain("15 seconds");
    }
}
