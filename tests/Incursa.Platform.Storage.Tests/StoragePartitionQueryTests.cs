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
public sealed class StoragePartitionQueryTests
{
    [Fact]
    public void All_ProducesAllMode()
    {
        StoragePartitionQuery query = StoragePartitionQuery.All(pageSizeHint: 50);

        query.Mode.ShouldBe(StoragePartitionQueryMode.All);
        query.PageSizeHint.ShouldBe(50);
    }

    [Fact]
    public void WithPrefix_ProducesPrefixMode()
    {
        StoragePartitionQuery query = StoragePartitionQuery.WithPrefix("cust:");

        query.Mode.ShouldBe(StoragePartitionQueryMode.Prefix);
        query.RowKeyPrefix.ShouldBe("cust:");
    }

    [Fact]
    public void WithinRange_RequiresAtLeastOneBound()
    {
        Should.Throw<ArgumentException>(() => StoragePartitionQuery.WithinRange(null, null));
    }

    [Fact]
    public void PageSizeHint_MustBePositive()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => StoragePartitionQuery.All(pageSizeHint: 0));
    }
}
