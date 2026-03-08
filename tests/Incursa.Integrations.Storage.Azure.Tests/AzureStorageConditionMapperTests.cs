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

using Azure;
using Incursa.Platform.Storage;

namespace Incursa.Integrations.Storage.Azure.Tests;

[Trait("Category", "Unit")]
public sealed class AzureStorageConditionMapperTests
{
    [Fact]
    public void ToBlobRequestConditions_MapsIfMatchToAzureETag()
    {
        StorageWriteCondition condition = StorageWriteCondition.IfMatch(new StorageETag("\"etag-1\""));

        var blobConditions = AzureStorageConditionMapper.ToBlobRequestConditions(condition);

        blobConditions.ShouldNotBeNull();
        blobConditions!.IfMatch.ToString().ShouldBe("\"etag-1\"");
    }

    [Fact]
    public void ToBlobRequestConditions_MapsIfNotExistsToIfNoneMatchWildcard()
    {
        var blobConditions = AzureStorageConditionMapper.ToBlobRequestConditions(StorageWriteCondition.IfNotExists());

        blobConditions.ShouldNotBeNull();
        blobConditions!.IfNoneMatch.ShouldBe(ETag.All);
    }

    [Fact]
    public void RequireAzureETag_ThrowsWhenConditionHasNoETag()
    {
        Should.Throw<StorageException>(() =>
            AzureStorageConditionMapper.RequireAzureETag(StorageWriteCondition.Unconditional()));
    }
}
