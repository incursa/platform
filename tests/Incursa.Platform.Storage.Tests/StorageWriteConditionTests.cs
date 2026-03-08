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
public sealed class StorageWriteConditionTests
{
    [Fact]
    public void Unconditional_ProducesUnconditionalCondition()
    {
        StorageWriteCondition condition = StorageWriteCondition.Unconditional();

        condition.Kind.ShouldBe(StorageWriteConditionKind.Unconditional);
        condition.ETag.ShouldBeNull();
    }

    [Fact]
    public void IfMatch_PreservesOpaqueETag()
    {
        StorageWriteCondition condition = StorageWriteCondition.IfMatch(new StorageETag("etag-1"));

        condition.Kind.ShouldBe(StorageWriteConditionKind.IfMatch);
        condition.ETag.ShouldBe(new StorageETag("etag-1"));
    }

    [Fact]
    public void IfNotExists_DoesNotCarryETag()
    {
        StorageWriteCondition condition = StorageWriteCondition.IfNotExists();

        condition.Kind.ShouldBe(StorageWriteConditionKind.IfNotExists);
        condition.ETag.ShouldBeNull();
    }
}
