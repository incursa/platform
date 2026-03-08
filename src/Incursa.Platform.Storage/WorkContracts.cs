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
/// Represents a work item that can be enqueued and claimed for processing.
/// </summary>
/// <typeparam name="TWorkItem">The work-item payload type.</typeparam>
public sealed record WorkItem<TWorkItem>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WorkItem{TWorkItem}"/> class.
    /// </summary>
    public WorkItem(
        string id,
        TWorkItem payload,
        string? correlationId = null,
        string? idempotencyKey = null,
        string? schemaVersion = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        Id = id;
        Payload = payload;
        CorrelationId = correlationId;
        IdempotencyKey = idempotencyKey;
        SchemaVersion = schemaVersion;
        Metadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.Ordinal)
            : new Dictionary<string, string>(metadata, StringComparer.Ordinal);
    }

    /// <summary>
    /// Gets the stable work-item identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the work-item payload.
    /// </summary>
    public TWorkItem Payload { get; }

    /// <summary>
    /// Gets the optional correlation identifier.
    /// </summary>
    public string? CorrelationId { get; }

    /// <summary>
    /// Gets the optional idempotency identifier.
    /// </summary>
    public string? IdempotencyKey { get; }

    /// <summary>
    /// Gets the optional schema version.
    /// </summary>
    public string? SchemaVersion { get; }

    /// <summary>
    /// Gets additional metadata.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }
}

/// <summary>
/// Represents enqueue-time options for a work item.
/// </summary>
public sealed record WorkEnqueueOptions
{
    private TimeSpan? initialVisibilityDelay;

    /// <summary>
    /// Gets or sets the initial invisibility delay applied to the message.
    /// </summary>
    public TimeSpan? InitialVisibilityDelay
    {
        get => initialVisibilityDelay;
        init
        {
            if (value < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Visibility delay must be zero or positive.");
            }

            initialVisibilityDelay = value;
        }
    }
}

/// <summary>
/// Represents claim-time options for work retrieval.
/// </summary>
public sealed record WorkClaimOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WorkClaimOptions"/> class.
    /// </summary>
    /// <param name="visibilityTimeout">The lease duration applied to a claimed item.</param>
    public WorkClaimOptions(TimeSpan visibilityTimeout)
    {
        if (visibilityTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(visibilityTimeout), "Visibility timeout must be positive.");
        }

        VisibilityTimeout = visibilityTimeout;
    }

    /// <summary>
    /// Gets the lease duration applied to the claimed item.
    /// </summary>
    public TimeSpan VisibilityTimeout { get; }
}

/// <summary>
/// Represents release-time options for a claimed work item.
/// </summary>
public sealed record WorkReleaseOptions
{
    private TimeSpan? visibilityDelay;

    /// <summary>
    /// Gets or sets the delay before the work item becomes visible again.
    /// </summary>
    public TimeSpan? VisibilityDelay
    {
        get => visibilityDelay;
        init
        {
            if (value < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Visibility delay must be zero or positive.");
            }

            visibilityDelay = value;
        }
    }
}

/// <summary>
/// Represents a claimed work item.
/// </summary>
/// <typeparam name="TWorkItem">The work-item payload type.</typeparam>
public sealed record ClaimedWorkItem<TWorkItem>(
    WorkItem<TWorkItem> Item,
    WorkClaimToken ClaimToken,
    DateTimeOffset ClaimedUntilUtc,
    int DeliveryCount);
