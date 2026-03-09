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
[Trait("Category", "Smoke")]
public sealed class StorageKeyPrimitiveTests
{
    [Fact]
    public void PartitionKey_RequiresValue()
    {
        Should.Throw<ArgumentException>(() => new StoragePartitionKey(string.Empty));
    }

    [Fact]
    public void RecordKey_ToString_IncludesPartitionAndRow()
    {
        StorageRecordKey key = new(new StoragePartitionKey("customers"), new StorageRowKey("42"));

        key.ToString().ShouldBe("customers/42");
    }

    [Fact]
    public void PayloadKey_RequiresScopeAndName()
    {
        Should.Throw<ArgumentException>(() => new StoragePayloadKey("scope", string.Empty));
        Should.Throw<ArgumentException>(() => new StoragePayloadKey(string.Empty, "payload"));
    }

    [Fact]
    public void ETag_RequiresOpaqueValue()
    {
        Should.Throw<ArgumentException>(() => new StorageETag(" "));
    }
}
