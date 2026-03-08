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
/// Defines the optimistic concurrency rule used for a storage mutation.
/// </summary>
public enum StorageWriteConditionKind
{
    /// <summary>
    /// No precondition is applied.
    /// </summary>
    Unconditional = 0,

    /// <summary>
    /// The mutation may proceed only when the current provider-managed ETag matches the supplied value.
    /// </summary>
    IfMatch = 1,

    /// <summary>
    /// The mutation may proceed only when the target does not already exist.
    /// </summary>
    IfNotExists = 2,
}

/// <summary>
/// Defines the write shape used by record-store mutations.
/// </summary>
public enum StorageWriteMode
{
    /// <summary>
    /// Writes an exact replacement for the record at the supplied key.
    /// </summary>
    Put = 0,

    /// <summary>
    /// Inserts or replaces the record at the supplied key.
    /// </summary>
    Upsert = 1,
}

/// <summary>
/// Defines the consistency intent of a multi-entity storage operation.
/// </summary>
public enum StorageConsistencyMode
{
    /// <summary>
    /// The caller expects a single-partition atomic mutation.
    /// </summary>
    SinglePartitionAtomic = 0,

    /// <summary>
    /// The caller expects cross-partition eventual propagation rather than a distributed transaction.
    /// </summary>
    CrossPartitionEventuallyConsistent = 1,
}

/// <summary>
/// Represents an optimistic concurrency precondition.
/// </summary>
public readonly record struct StorageWriteCondition
{
    private StorageWriteCondition(StorageWriteConditionKind kind, StorageETag? etag)
    {
        if (kind == StorageWriteConditionKind.IfMatch && etag is null)
        {
            throw new ArgumentException("An ETag is required when using an IfMatch condition.", nameof(etag));
        }

        if (kind != StorageWriteConditionKind.IfMatch && etag is not null)
        {
            throw new ArgumentException("ETag is supported only for IfMatch conditions.", nameof(etag));
        }

        Kind = kind;
        ETag = etag;
    }

    /// <summary>
    /// Gets the condition kind.
    /// </summary>
    public StorageWriteConditionKind Kind { get; }

    /// <summary>
    /// Gets the expected ETag when <see cref="Kind"/> is <see cref="StorageWriteConditionKind.IfMatch"/>.
    /// </summary>
    public StorageETag? ETag { get; }

    /// <summary>
    /// Creates an unconditional write condition.
    /// </summary>
    /// <returns>The write condition.</returns>
    public static StorageWriteCondition Unconditional() => new(StorageWriteConditionKind.Unconditional, null);

    /// <summary>
    /// Creates a write condition that requires the target to match the supplied ETag.
    /// </summary>
    /// <param name="etag">The expected provider-managed ETag.</param>
    /// <returns>The write condition.</returns>
    public static StorageWriteCondition IfMatch(StorageETag etag) => new(StorageWriteConditionKind.IfMatch, etag);

    /// <summary>
    /// Creates a write condition that requires the target not to exist.
    /// </summary>
    /// <returns>The write condition.</returns>
    public static StorageWriteCondition IfNotExists() => new(StorageWriteConditionKind.IfNotExists, null);
}
