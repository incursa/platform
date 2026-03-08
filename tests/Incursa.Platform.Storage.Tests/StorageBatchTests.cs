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

namespace Incursa.Platform.Storage.Tests;

[Trait("Category", "Unit")]
public sealed class StorageBatchTests
{
    [Fact]
    public void Batch_RequiresAtLeastOneOperation()
    {
        Should.Throw<ArgumentException>(() => new StorageBatch<string>(Array.Empty<StorageBatchOperation<string>>()));
    }

    [Fact]
    public void SinglePartitionAtomicBatch_RejectsMultiplePartitions()
    {
        var operations = new[]
        {
            StorageBatchOperation<string>.Put(
                new StorageRecordKey(new StoragePartitionKey("p1"), new StorageRowKey("1")),
                "first",
                StorageWriteCondition.Unconditional()),
            StorageBatchOperation<string>.Put(
                new StorageRecordKey(new StoragePartitionKey("p2"), new StorageRowKey("2")),
                "second",
                StorageWriteCondition.Unconditional()),
        };

        Should.Throw<ArgumentException>(() => new StorageBatch<string>(operations));
    }

    [Fact]
    public void EventuallyConsistentBatch_AllowsMultiplePartitions()
    {
        var operations = new[]
        {
            StorageBatchOperation<string>.Upsert(
                new StorageRecordKey(new StoragePartitionKey("p1"), new StorageRowKey("1")),
                "first",
                StorageWriteCondition.Unconditional()),
            StorageBatchOperation<string>.Delete(
                new StorageRecordKey(new StoragePartitionKey("p2"), new StorageRowKey("2")),
                StorageWriteCondition.Unconditional()),
        };

        StorageBatch<string> batch = new(operations, StorageConsistencyMode.CrossPartitionEventuallyConsistent);

        batch.ConsistencyMode.ShouldBe(StorageConsistencyMode.CrossPartitionEventuallyConsistent);
        batch.Operations.Count.ShouldBe(2);
    }
}
