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

namespace Incursa.Platform.Storage;

/// <summary>
/// Represents caller-supplied options for payload writes.
/// </summary>
public sealed record PayloadWriteOptions
{
    /// <summary>
    /// Gets or sets the content type.
    /// </summary>
    public string? ContentType { get; init; }

    /// <summary>
    /// Gets or sets the caller-defined schema version.
    /// </summary>
    public string? SchemaVersion { get; init; }

    /// <summary>
    /// Gets or sets the caller-defined checksum or hash value.
    /// </summary>
    public string? Checksum { get; init; }

    /// <summary>
    /// Gets or sets additional provider-neutral metadata.
    /// </summary>
    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}

/// <summary>
/// Represents payload metadata that can be queried without reading the payload body.
/// </summary>
public sealed record PayloadMetadata
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PayloadMetadata"/> class.
    /// </summary>
    public PayloadMetadata(
        StoragePayloadKey key,
        long contentLength,
        StorageETag etag,
        string? contentType = null,
        string? schemaVersion = null,
        string? checksum = null,
        DateTimeOffset? createdUtc = null,
        DateTimeOffset? lastModifiedUtc = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        Key = key;
        ContentLength = contentLength;
        ETag = etag;
        ContentType = contentType;
        SchemaVersion = schemaVersion;
        Checksum = checksum;
        CreatedUtc = createdUtc;
        LastModifiedUtc = lastModifiedUtc;
        Metadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.Ordinal)
            : new Dictionary<string, string>(metadata, StringComparer.Ordinal);
    }

    /// <summary>
    /// Gets the payload key.
    /// </summary>
    public StoragePayloadKey Key { get; }

    /// <summary>
    /// Gets the payload length in bytes.
    /// </summary>
    public long ContentLength { get; }

    /// <summary>
    /// Gets the current provider-managed ETag.
    /// </summary>
    public StorageETag ETag { get; }

    /// <summary>
    /// Gets the content type, when available.
    /// </summary>
    public string? ContentType { get; }

    /// <summary>
    /// Gets the caller-defined schema version, when available.
    /// </summary>
    public string? SchemaVersion { get; }

    /// <summary>
    /// Gets the caller-defined checksum or hash value, when available.
    /// </summary>
    public string? Checksum { get; }

    /// <summary>
    /// Gets the provider-supplied created timestamp, when available.
    /// </summary>
    public DateTimeOffset? CreatedUtc { get; }

    /// <summary>
    /// Gets the provider-supplied last-modified timestamp, when available.
    /// </summary>
    public DateTimeOffset? LastModifiedUtc { get; }

    /// <summary>
    /// Gets additional provider-neutral metadata.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }
}

/// <summary>
/// Represents a typed payload read result.
/// </summary>
/// <typeparam name="TPayload">The payload type.</typeparam>
public sealed record PayloadReadResult<TPayload>(PayloadMetadata Metadata, TPayload Value);

/// <summary>
/// Represents a streaming payload read result.
/// </summary>
public sealed class PayloadStreamResult : IAsyncDisposable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PayloadStreamResult"/> class.
    /// </summary>
    /// <param name="metadata">The payload metadata.</param>
    /// <param name="content">The payload content stream.</param>
    public PayloadStreamResult(PayloadMetadata metadata, Stream content)
    {
        Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
        Content = content ?? throw new ArgumentNullException(nameof(content));
    }

    /// <summary>
    /// Gets the payload metadata.
    /// </summary>
    public PayloadMetadata Metadata { get; }

    /// <summary>
    /// Gets the payload content stream.
    /// </summary>
    public Stream Content { get; }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        if (Content is IAsyncDisposable asyncDisposable)
        {
            return asyncDisposable.DisposeAsync();
        }

        Content.Dispose();
        return ValueTask.CompletedTask;
    }
}
