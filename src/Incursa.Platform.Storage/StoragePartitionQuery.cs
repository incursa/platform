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
/// Represents the supported row-key query shapes for a single partition.
/// </summary>
public sealed record StoragePartitionQuery
{
    private StoragePartitionQuery(
        string? rowKeyPrefix,
        string? startRowKeyInclusive,
        string? endRowKeyExclusive,
        int? pageSizeHint)
    {
        if (pageSizeHint is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pageSizeHint), "Page size hints must be positive.");
        }

        if (rowKeyPrefix is not null && (startRowKeyInclusive is not null || endRowKeyExclusive is not null))
        {
            throw new ArgumentException(
                "Prefix queries cannot also define bounded range values.",
                nameof(rowKeyPrefix));
        }

        if (rowKeyPrefix is null && startRowKeyInclusive is null && endRowKeyExclusive is null)
        {
            Mode = StoragePartitionQueryMode.All;
        }
        else if (rowKeyPrefix is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(rowKeyPrefix);
            Mode = StoragePartitionQueryMode.Prefix;
        }
        else
        {
            Mode = StoragePartitionQueryMode.Range;
        }

        RowKeyPrefix = rowKeyPrefix;
        StartRowKeyInclusive = startRowKeyInclusive;
        EndRowKeyExclusive = endRowKeyExclusive;
        PageSizeHint = pageSizeHint;
    }

    /// <summary>
    /// Gets the query mode.
    /// </summary>
    public StoragePartitionQueryMode Mode { get; }

    /// <summary>
    /// Gets the row-key prefix when <see cref="Mode"/> is <see cref="StoragePartitionQueryMode.Prefix"/>.
    /// </summary>
    public string? RowKeyPrefix { get; }

    /// <summary>
    /// Gets the inclusive row-key lower bound when <see cref="Mode"/> is <see cref="StoragePartitionQueryMode.Range"/>.
    /// </summary>
    public string? StartRowKeyInclusive { get; }

    /// <summary>
    /// Gets the exclusive row-key upper bound when <see cref="Mode"/> is <see cref="StoragePartitionQueryMode.Range"/>.
    /// </summary>
    public string? EndRowKeyExclusive { get; }

    /// <summary>
    /// Gets the optional provider hint for page size.
    /// </summary>
    public int? PageSizeHint { get; }

    /// <summary>
    /// Creates a full-partition query.
    /// </summary>
    /// <param name="pageSizeHint">Optional provider hint for page size.</param>
    /// <returns>The query.</returns>
    public static StoragePartitionQuery All(int? pageSizeHint = null) => new(null, null, null, pageSizeHint);

    /// <summary>
    /// Creates a prefix query within a single partition.
    /// </summary>
    /// <param name="rowKeyPrefix">The row-key prefix.</param>
    /// <param name="pageSizeHint">Optional provider hint for page size.</param>
    /// <returns>The query.</returns>
    public static StoragePartitionQuery WithPrefix(string rowKeyPrefix, int? pageSizeHint = null) =>
        new(rowKeyPrefix, null, null, pageSizeHint);

    /// <summary>
    /// Creates a bounded row-key range query within a single partition.
    /// </summary>
    /// <param name="startRowKeyInclusive">The inclusive lower bound, if any.</param>
    /// <param name="endRowKeyExclusive">The exclusive upper bound, if any.</param>
    /// <param name="pageSizeHint">Optional provider hint for page size.</param>
    /// <returns>The query.</returns>
    public static StoragePartitionQuery WithinRange(
        string? startRowKeyInclusive,
        string? endRowKeyExclusive,
        int? pageSizeHint = null)
    {
        if (startRowKeyInclusive is null && endRowKeyExclusive is null)
        {
            throw new ArgumentException(
                "A bounded range query must define at least one row-key bound.",
                nameof(startRowKeyInclusive));
        }

        return new(null, startRowKeyInclusive, endRowKeyExclusive, pageSizeHint);
    }
}

/// <summary>
/// Represents the supported partition query modes.
/// </summary>
public enum StoragePartitionQueryMode
{
    /// <summary>
    /// Reads the full partition.
    /// </summary>
    All = 0,

    /// <summary>
    /// Reads a row-key prefix within the partition.
    /// </summary>
    Prefix = 1,

    /// <summary>
    /// Reads a bounded row-key range within the partition.
    /// </summary>
    Range = 2,
}
