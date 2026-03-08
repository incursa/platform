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

using Azure.Identity;

namespace Incursa.Integrations.Storage.Azure;

/// <summary>
/// Configures the Azure-backed storage provider.
/// </summary>
public sealed class AzureStorageOptions
{
    /// <summary>
    /// Gets or sets the Azure Storage connection string.
    /// When provided, service URIs are ignored.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Gets or sets the blob service endpoint used with <see cref="DefaultAzureCredential"/>.
    /// </summary>
    public Uri? BlobServiceUri { get; set; }

    /// <summary>
    /// Gets or sets the queue service endpoint used with <see cref="DefaultAzureCredential"/>.
    /// </summary>
    public Uri? QueueServiceUri { get; set; }

    /// <summary>
    /// Gets or sets the table service endpoint used with <see cref="DefaultAzureCredential"/>.
    /// </summary>
    public Uri? TableServiceUri { get; set; }

    /// <summary>
    /// Gets or sets the record table prefix.
    /// </summary>
    public string RecordTablePrefix { get; set; } = "Record";

    /// <summary>
    /// Gets or sets the lookup table prefix.
    /// </summary>
    public string LookupTablePrefix { get; set; } = "Lookup";

    /// <summary>
    /// Gets or sets explicit record-table overrides keyed by the CLR type full name or simple name.
    /// </summary>
    public Dictionary<string, string> RecordTables { get; set; } = new(StringComparer.Ordinal);

    /// <summary>
    /// Gets or sets explicit lookup-table overrides keyed by the CLR type full name or simple name.
    /// </summary>
    public Dictionary<string, string> LookupTables { get; set; } = new(StringComparer.Ordinal);

    /// <summary>
    /// Gets or sets the payload container name.
    /// </summary>
    public string PayloadContainerName { get; set; } = "payloads";

    /// <summary>
    /// Gets or sets the container name used for work-item payload overflow.
    /// </summary>
    public string WorkPayloadContainerName { get; set; } = "work-payloads";

    /// <summary>
    /// Gets or sets the queue prefix used for work stores.
    /// </summary>
    public string WorkQueuePrefix { get; set; } = "work";

    /// <summary>
    /// Gets or sets explicit work-queue overrides keyed by the CLR type full name or simple name.
    /// </summary>
    public Dictionary<string, string> WorkQueues { get; set; } = new(StringComparer.Ordinal);

    /// <summary>
    /// Gets or sets the container used for coordination leases.
    /// </summary>
    public string CoordinationContainerName { get; set; } = "coordination";

    /// <summary>
    /// Gets or sets the table used for coordination markers and checkpoints.
    /// </summary>
    public string CoordinationTableName { get; set; } = "Coordination";

    /// <summary>
    /// Gets or sets a value indicating whether missing tables, containers, and queues should be created on first use.
    /// </summary>
    public bool CreateResourcesIfMissing { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum serialized queue message size to keep inline before using a blob reference.
    /// </summary>
    public int WorkMessageInlineThresholdBytes { get; set; } = 48 * 1024;

    /// <summary>
    /// Gets or sets the serializer options used for payloads, records, lookups, checkpoints, and work envelopes.
    /// </summary>
    public JsonSerializerOptions SerializerOptions { get; set; } = CreateDefaultSerializerOptions();

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureStorageOptions"/> class.
    /// </summary>
    public AzureStorageOptions()
    {
    }

    internal AzureStorageOptions(AzureStorageOptions source)
    {
        CopyFrom(source);
    }

    internal void CopyFrom(AzureStorageOptions source)
    {
        ArgumentNullException.ThrowIfNull(source);

        ConnectionString = source.ConnectionString;
        BlobServiceUri = source.BlobServiceUri;
        QueueServiceUri = source.QueueServiceUri;
        TableServiceUri = source.TableServiceUri;
        RecordTablePrefix = source.RecordTablePrefix;
        LookupTablePrefix = source.LookupTablePrefix;
        RecordTables = new Dictionary<string, string>(source.RecordTables, StringComparer.Ordinal);
        LookupTables = new Dictionary<string, string>(source.LookupTables, StringComparer.Ordinal);
        PayloadContainerName = source.PayloadContainerName;
        WorkPayloadContainerName = source.WorkPayloadContainerName;
        WorkQueuePrefix = source.WorkQueuePrefix;
        WorkQueues = new Dictionary<string, string>(source.WorkQueues, StringComparer.Ordinal);
        CoordinationContainerName = source.CoordinationContainerName;
        CoordinationTableName = source.CoordinationTableName;
        CreateResourcesIfMissing = source.CreateResourcesIfMissing;
        WorkMessageInlineThresholdBytes = source.WorkMessageInlineThresholdBytes;
        SerializerOptions = new JsonSerializerOptions(source.SerializerOptions);
    }

    internal AzureStorageOptions Clone() => new(this);

    private static JsonSerializerOptions CreateDefaultSerializerOptions()
    {
        JsonSerializerOptions options = new(JsonSerializerDefaults.Web);
        options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        return options;
    }
}
