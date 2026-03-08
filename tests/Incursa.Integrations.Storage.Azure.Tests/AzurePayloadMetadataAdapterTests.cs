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
public sealed class AzurePayloadMetadataAdapterTests
{
    [Fact]
    public void BuildMetadataDictionary_NormalizesKeysAndIncludesProviderMetadata()
    {
        PayloadWriteOptions options = new()
        {
            SchemaVersion = "v3",
            Checksum = "ABC123",
            Metadata = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Correlation-Id"] = "corr-1",
                ["Tenant Name"] = "northwind",
            },
        };

        IDictionary<string, string> metadata = AzurePayloadMetadataAdapter.BuildMetadataDictionary(options);

        metadata["correlationid"].ShouldBe("corr-1");
        metadata["tenantname"].ShouldBe("northwind");
        metadata[AzureStorageExceptionHelper.MetadataSchemaVersionKey].ShouldBe("v3");
        metadata[AzureStorageExceptionHelper.MetadataChecksumKey].ShouldBe("ABC123");
    }
}
