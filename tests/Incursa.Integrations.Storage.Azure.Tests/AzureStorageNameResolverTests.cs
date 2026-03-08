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
public sealed class AzureStorageNameResolverTests
{
    [Fact]
    public void GetRecordTableName_UsesExplicitOverride()
    {
        AzureStorageOptions options = AzureStorageTestOptions.CreateUnitOptions();
        options.RecordTables[typeof(SampleRecord).FullName!] = "SampleRecords";

        AzureStorageNameResolver resolver = new(options);

        resolver.GetRecordTableName(typeof(SampleRecord)).ShouldBe("SampleRecords");
    }

    [Fact]
    public void GetWorkQueueName_SanitizesAndStabilizesGeneratedName()
    {
        AzureStorageOptions options = AzureStorageTestOptions.CreateUnitOptions();
        AzureStorageNameResolver resolver = new(options);

        string first = resolver.GetWorkQueueName(typeof(QueueTypeWithSymbols123));
        string second = resolver.GetWorkQueueName(typeof(QueueTypeWithSymbols123));

        first.ShouldBe(second);
        first.ShouldStartWith($"{options.WorkQueuePrefix}-");
        first.ShouldContain("queuetypewithsymbols123");
        first.ShouldSatisfyAllConditions(
            queueName => queueName.Length.ShouldBeLessThanOrEqualTo(63),
            queueName => queueName.ShouldBe(queueName.ToLowerInvariant()));
    }

    [Fact]
    public void GetLeaseBlobName_EscapesPartitionAndRowKeys()
    {
        AzureStorageNameResolver resolver = new(AzureStorageTestOptions.CreateUnitOptions());
        StorageRecordKey key = new(new StoragePartitionKey("alpha/beta"), new StorageRowKey("row key"));

        string blobName = resolver.GetLeaseBlobName(key);

        blobName.ShouldBe("leases/alpha%2Fbeta/row%20key");
    }

    private sealed class QueueTypeWithSymbols123;
}
