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
/// Represents a logical partition identifier for partition-aware storage backends.
/// </summary>
public readonly record struct StoragePartitionKey
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StoragePartitionKey"/> struct.
    /// </summary>
    /// <param name="value">The logical partition key value.</param>
    public StoragePartitionKey(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    /// <summary>
    /// Gets the underlying partition key value.
    /// </summary>
    public string Value { get; }

    /// <inheritdoc />
    public override string ToString() => Value;
}

/// <summary>
/// Represents a logical row key within a partition.
/// </summary>
public readonly record struct StorageRowKey
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StorageRowKey"/> struct.
    /// </summary>
    /// <param name="value">The logical row key value.</param>
    public StorageRowKey(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    /// <summary>
    /// Gets the underlying row key value.
    /// </summary>
    public string Value { get; }

    /// <inheritdoc />
    public override string ToString() => Value;
}

/// <summary>
/// Represents an exact record key composed of a partition key and a row key.
/// </summary>
public readonly record struct StorageRecordKey
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StorageRecordKey"/> struct.
    /// </summary>
    /// <param name="partitionKey">The partition key.</param>
    /// <param name="rowKey">The row key.</param>
    public StorageRecordKey(StoragePartitionKey partitionKey, StorageRowKey rowKey)
    {
        PartitionKey = partitionKey;
        RowKey = rowKey;
    }

    /// <summary>
    /// Gets the partition key.
    /// </summary>
    public StoragePartitionKey PartitionKey { get; }

    /// <summary>
    /// Gets the row key.
    /// </summary>
    public StorageRowKey RowKey { get; }

    /// <inheritdoc />
    public override string ToString() => $"{PartitionKey}/{RowKey}";
}

/// <summary>
/// Represents a provider-neutral payload identifier.
/// </summary>
public readonly record struct StoragePayloadKey
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StoragePayloadKey"/> struct.
    /// </summary>
    /// <param name="scope">The logical payload scope.</param>
    /// <param name="name">The payload name within the scope.</param>
    public StoragePayloadKey(string scope, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scope);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Scope = scope;
        Name = name;
    }

    /// <summary>
    /// Gets the logical payload scope.
    /// </summary>
    public string Scope { get; }

    /// <summary>
    /// Gets the logical payload name.
    /// </summary>
    public string Name { get; }

    /// <inheritdoc />
    public override string ToString() => $"{Scope}/{Name}";
}

/// <summary>
/// Represents an opaque provider-managed concurrency token.
/// </summary>
public readonly record struct StorageETag
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StorageETag"/> struct.
    /// </summary>
    /// <param name="value">The opaque ETag value.</param>
    public StorageETag(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    /// <summary>
    /// Gets the underlying opaque ETag value.
    /// </summary>
    public string Value { get; }

    /// <inheritdoc />
    public override string ToString() => Value;
}

/// <summary>
/// Represents an opaque claim token for queue-backed work items.
/// </summary>
public readonly record struct WorkClaimToken
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WorkClaimToken"/> struct.
    /// </summary>
    /// <param name="value">The opaque claim token value.</param>
    public WorkClaimToken(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    /// <summary>
    /// Gets the opaque claim token value.
    /// </summary>
    public string Value { get; }

    /// <inheritdoc />
    public override string ToString() => Value;
}

/// <summary>
/// Represents an opaque coordination lease token.
/// </summary>
public readonly record struct CoordinationLeaseToken
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CoordinationLeaseToken"/> struct.
    /// </summary>
    /// <param name="value">The opaque lease token value.</param>
    public CoordinationLeaseToken(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    /// <summary>
    /// Gets the opaque lease token value.
    /// </summary>
    public string Value { get; }

    /// <inheritdoc />
    public override string ToString() => Value;
}
