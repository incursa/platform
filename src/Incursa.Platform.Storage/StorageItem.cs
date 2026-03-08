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
/// Represents a keyed storage value returned by a record-like store.
/// </summary>
/// <typeparam name="TValue">The stored value type.</typeparam>
public sealed record StorageItem<TValue>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StorageItem{TValue}"/> class.
    /// </summary>
    /// <param name="key">The record key.</param>
    /// <param name="value">The stored value.</param>
    /// <param name="etag">The current provider-managed ETag.</param>
    /// <param name="lastModifiedUtc">The last modified timestamp, when available.</param>
    public StorageItem(
        StorageRecordKey key,
        TValue value,
        StorageETag etag,
        DateTimeOffset? lastModifiedUtc = null)
    {
        Key = key;
        Value = value;
        ETag = etag;
        LastModifiedUtc = lastModifiedUtc;
    }

    /// <summary>
    /// Gets the record key.
    /// </summary>
    public StorageRecordKey Key { get; }

    /// <summary>
    /// Gets the stored value.
    /// </summary>
    public TValue Value { get; }

    /// <summary>
    /// Gets the current provider-managed ETag.
    /// </summary>
    public StorageETag ETag { get; }

    /// <summary>
    /// Gets the last modified timestamp, when the provider can supply one.
    /// </summary>
    public DateTimeOffset? LastModifiedUtc { get; }
}
