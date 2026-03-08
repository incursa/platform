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
using Azure.Storage.Blobs.Models;
using Incursa.Platform.Storage;

namespace Incursa.Integrations.Storage.Azure;

internal static class AzureStorageConditionMapper
{
    public static ETag RequireAzureETag(StorageWriteCondition condition)
    {
        if (condition.ETag is not StorageETag etag)
        {
            throw new StorageException("The storage write condition requires an ETag.");
        }

        return new ETag(etag.Value);
    }

    public static BlobRequestConditions? ToBlobRequestConditions(StorageWriteCondition condition)
    {
        return condition.Kind switch
        {
            StorageWriteConditionKind.Unconditional => null,
            StorageWriteConditionKind.IfMatch => new BlobRequestConditions { IfMatch = RequireAzureETag(condition) },
            StorageWriteConditionKind.IfNotExists => new BlobRequestConditions { IfNoneMatch = ETag.All },
            _ => null,
        };
    }
}

internal static class AzurePayloadMetadataAdapter
{
    public static IDictionary<string, string> BuildMetadataDictionary(PayloadWriteOptions writeOptions)
    {
        ArgumentNullException.ThrowIfNull(writeOptions);

        Dictionary<string, string> metadata = new(StringComparer.Ordinal);
        if (writeOptions.Metadata is not null)
        {
            foreach ((string key, string value) in writeOptions.Metadata)
            {
                metadata[NormalizeMetadataKey(key)] = value;
            }
        }

        if (!string.IsNullOrWhiteSpace(writeOptions.SchemaVersion))
        {
            metadata[AzureStorageExceptionHelper.MetadataSchemaVersionKey] = writeOptions.SchemaVersion;
        }

        if (!string.IsNullOrWhiteSpace(writeOptions.Checksum))
        {
            metadata[AzureStorageExceptionHelper.MetadataChecksumKey] = writeOptions.Checksum;
        }

        return metadata;
    }

    public static PayloadMetadata Build(StoragePayloadKey key, BlobProperties properties)
    {
        ArgumentNullException.ThrowIfNull(properties);

        Dictionary<string, string> metadata = new(properties.Metadata, StringComparer.Ordinal);
        return new PayloadMetadata(
            key,
            properties.ContentLength,
            new StorageETag(properties.ETag.ToString()),
            properties.ContentType,
            metadata.TryGetValue(AzureStorageExceptionHelper.MetadataSchemaVersionKey, out string? schemaVersion) ? schemaVersion : null,
            metadata.TryGetValue(AzureStorageExceptionHelper.MetadataChecksumKey, out string? checksum) ? checksum : null,
            properties.CreatedOn,
            properties.LastModified,
            metadata);
    }

    public static PayloadMetadata Build(StoragePayloadKey key, BlobDownloadDetails details)
    {
        ArgumentNullException.ThrowIfNull(details);

        Dictionary<string, string> metadata = new(details.Metadata, StringComparer.Ordinal);
        return new PayloadMetadata(
            key,
            details.ContentLength,
            new StorageETag(details.ETag.ToString()),
            details.ContentType,
            metadata.TryGetValue(AzureStorageExceptionHelper.MetadataSchemaVersionKey, out string? schemaVersion) ? schemaVersion : null,
            metadata.TryGetValue(AzureStorageExceptionHelper.MetadataChecksumKey, out string? checksum) ? checksum : null,
            details.CreatedOn,
            details.LastModified,
            metadata);
    }

    private static string NormalizeMetadataKey(string key)
    {
        string normalized = new(key.ToLowerInvariant().Where(c => char.IsLetterOrDigit(c)).ToArray());
        return string.IsNullOrEmpty(normalized) ? "meta" : normalized;
    }
}
