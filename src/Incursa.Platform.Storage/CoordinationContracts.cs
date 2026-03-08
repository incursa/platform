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
/// Represents a lease-acquisition request for coordination scenarios.
/// </summary>
public sealed record CoordinationLeaseRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CoordinationLeaseRequest"/> class.
    /// </summary>
    /// <param name="key">The coordination key.</param>
    /// <param name="owner">The logical owner identifier.</param>
    /// <param name="duration">The requested lease duration.</param>
    public CoordinationLeaseRequest(StorageRecordKey key, string owner, TimeSpan duration)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(owner);
        if (duration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(duration), "Lease duration must be positive.");
        }

        Key = key;
        Owner = owner;
        Duration = duration;
    }

    /// <summary>
    /// Gets the coordination key.
    /// </summary>
    public StorageRecordKey Key { get; }

    /// <summary>
    /// Gets the logical owner identifier.
    /// </summary>
    public string Owner { get; }

    /// <summary>
    /// Gets the requested lease duration.
    /// </summary>
    public TimeSpan Duration { get; }
}

/// <summary>
/// Represents an acquired coordination lease.
/// </summary>
public sealed record CoordinationLease(
    StorageRecordKey Key,
    string Owner,
    CoordinationLeaseToken Token,
    DateTimeOffset AcquiredUtc,
    DateTimeOffset ExpiresUtc,
    TimeSpan Duration);
