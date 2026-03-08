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

using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;
using Azure;
using Azure.Core;
using Azure.Data.Tables;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Incursa.Platform.Storage;
using Microsoft.Extensions.Options;

namespace Incursa.Integrations.Storage.Azure;

internal static class OptionsValidationHelper
{
    internal static void ValidateAndThrow<TOptions>(TOptions options, IValidateOptions<TOptions> validator)
        where TOptions : class
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(validator);

        ValidateOptionsResult validationResult = validator.Validate(Options.DefaultName, options);
        if (validationResult.Failed)
        {
            throw new OptionsValidationException(Options.DefaultName, typeof(TOptions), validationResult.Failures);
        }
    }
}

internal sealed class AzureStorageOptionsValidator : IValidateOptions<AzureStorageOptions>
{
    public ValidateOptionsResult Validate(string? name, AzureStorageOptions options)
    {
        if (options is null)
        {
            return ValidateOptionsResult.Fail("Options are required.");
        }

        if (string.IsNullOrWhiteSpace(options.ConnectionString) &&
            (options.BlobServiceUri is null || options.QueueServiceUri is null || options.TableServiceUri is null))
        {
            return ValidateOptionsResult.Fail("Provide either a connection string or all three service URIs for blobs, queues, and tables.");
        }

        if (options.SerializerOptions is null)
        {
            return ValidateOptionsResult.Fail("SerializerOptions must be provided.");
        }

        if (options.WorkMessageInlineThresholdBytes <= 0 || options.WorkMessageInlineThresholdBytes > 48 * 1024)
        {
            return ValidateOptionsResult.Fail("WorkMessageInlineThresholdBytes must be between 1 and 49152 bytes.");
        }

        if (!IsValidTablePrefix(options.RecordTablePrefix))
        {
            return ValidateOptionsResult.Fail("RecordTablePrefix must contain only letters and digits and start with a letter.");
        }

        if (!IsValidTablePrefix(options.LookupTablePrefix))
        {
            return ValidateOptionsResult.Fail("LookupTablePrefix must contain only letters and digits and start with a letter.");
        }

        if (!IsValidTableName(options.CoordinationTableName))
        {
            return ValidateOptionsResult.Fail("CoordinationTableName must be 3-63 characters, start with a letter, and contain only letters and digits.");
        }

        if (!IsValidBlobOrQueueName(options.PayloadContainerName))
        {
            return ValidateOptionsResult.Fail("PayloadContainerName must be a valid Azure container name.");
        }

        if (!IsValidBlobOrQueueName(options.WorkPayloadContainerName))
        {
            return ValidateOptionsResult.Fail("WorkPayloadContainerName must be a valid Azure container name.");
        }

        if (!IsValidBlobOrQueueName(options.CoordinationContainerName))
        {
            return ValidateOptionsResult.Fail("CoordinationContainerName must be a valid Azure container name.");
        }

        if (!IsValidBlobOrQueueName(options.WorkQueuePrefix))
        {
            return ValidateOptionsResult.Fail("WorkQueuePrefix must be a valid Azure queue-name prefix.");
        }

        if (options.RecordTables.Any(kvp => string.IsNullOrWhiteSpace(kvp.Key) || !IsValidTableName(kvp.Value)))
        {
            return ValidateOptionsResult.Fail("Record-table overrides must use non-empty keys and valid Azure table names.");
        }

        if (options.LookupTables.Any(kvp => string.IsNullOrWhiteSpace(kvp.Key) || !IsValidTableName(kvp.Value)))
        {
            return ValidateOptionsResult.Fail("Lookup-table overrides must use non-empty keys and valid Azure table names.");
        }

        if (options.WorkQueues.Any(kvp => string.IsNullOrWhiteSpace(kvp.Key) || !IsValidBlobOrQueueName(kvp.Value)))
        {
            return ValidateOptionsResult.Fail("Work-queue overrides must use non-empty keys and valid Azure queue names.");
        }

        return ValidateOptionsResult.Success;
    }

    private static bool IsValidTablePrefix(string value) =>
        !string.IsNullOrWhiteSpace(value) &&
        char.IsLetter(value[0]) &&
        value.All(char.IsLetterOrDigit);

    private static bool IsValidTableName(string value) =>
        !string.IsNullOrWhiteSpace(value) &&
        value.Length is >= 3 and <= 63 &&
        char.IsLetter(value[0]) &&
        value.All(char.IsLetterOrDigit);

    private static bool IsValidBlobOrQueueName(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length is < 3 or > 63)
        {
            return false;
        }

        if (value[0] == '-' || value[^1] == '-' || value.Contains("--", StringComparison.Ordinal))
        {
            return false;
        }

        return value.All(c => char.IsLower(c) || char.IsDigit(c) || c == '-');
    }
}

internal sealed class AzureStorageClientFactory
{
    private readonly BlobServiceClient blobServiceClient;
    private readonly QueueServiceClient queueServiceClient;
    private readonly TableServiceClient tableServiceClient;

    public AzureStorageClientFactory(AzureStorageOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            blobServiceClient = new BlobServiceClient(options.ConnectionString);
            queueServiceClient = new QueueServiceClient(options.ConnectionString);
            tableServiceClient = new TableServiceClient(options.ConnectionString);
            return;
        }

        TokenCredential credential = new DefaultAzureCredential();
        blobServiceClient = new BlobServiceClient(options.BlobServiceUri!, credential);
        queueServiceClient = new QueueServiceClient(options.QueueServiceUri!, credential);
        tableServiceClient = new TableServiceClient(options.TableServiceUri!, credential);
    }

    public BlobServiceClient BlobServiceClient => blobServiceClient;

    public QueueServiceClient QueueServiceClient => queueServiceClient;

    public TableServiceClient TableServiceClient => tableServiceClient;
}

internal sealed class AzureStorageNameResolver
{
    private readonly AzureStorageOptions options;

    public AzureStorageNameResolver(AzureStorageOptions options)
    {
        this.options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public string GetRecordTableName(Type type) => ResolveTableName(type, options.RecordTables, options.RecordTablePrefix);

    public string GetLookupTableName(Type type) => ResolveTableName(type, options.LookupTables, options.LookupTablePrefix);

    public string GetWorkQueueName(Type type) => ResolveQueueName(type, options.WorkQueues, options.WorkQueuePrefix);

    public string GetPayloadBlobName(StoragePayloadKey key)
    {
        return $"{Uri.EscapeDataString(key.Scope)}/{key.Name.TrimStart('/')}";
    }

    public string GetWorkPayloadBlobName(Type type, string workItemId)
    {
        return $"{SanitizeQueueName(type.Name)}-{ShortHash(type.FullName ?? type.Name)}/{Uri.EscapeDataString(workItemId)}-{Guid.NewGuid():N}.json";
    }

    public string GetLeaseBlobName(StorageRecordKey key)
    {
        return $"leases/{Uri.EscapeDataString(key.PartitionKey.Value)}/{Uri.EscapeDataString(key.RowKey.Value)}";
    }

    public string GetCoordinationPartitionKey(string kind, StorageRecordKey key)
    {
        return $"{kind}:{key.PartitionKey.Value}";
    }

    private static string ResolveTableName(Type type, IReadOnlyDictionary<string, string> overrides, string prefix)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (TryGetOverride(type, overrides, out string? explicitName))
        {
            return explicitName;
        }

        string suffix = $"{SanitizeTableName(type.Name)}{ShortHash(type.FullName ?? type.Name)}";
        return TruncateTableName($"{prefix}{suffix}");
    }

    private static string ResolveQueueName(Type type, IReadOnlyDictionary<string, string> overrides, string prefix)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (TryGetOverride(type, overrides, out string? explicitName))
        {
            return explicitName;
        }

        string suffix = $"{SanitizeQueueName(type.Name)}-{ShortHash(type.FullName ?? type.Name).ToLowerInvariant()}";
        return TruncateQueueName($"{prefix}-{suffix}");
    }

    private static bool TryGetOverride(
        Type type,
        IReadOnlyDictionary<string, string> overrides,
        [NotNullWhen(true)] out string? explicitName)
    {
        string fullName = type.FullName ?? type.Name;
        if (overrides.TryGetValue(fullName, out explicitName))
        {
            return true;
        }

        return overrides.TryGetValue(type.Name, out explicitName);
    }

    private static string SanitizeTableName(string value)
    {
        string sanitized = new(value.Where(char.IsLetterOrDigit).ToArray());
        return string.IsNullOrEmpty(sanitized) ? "Type" : sanitized;
    }

    private static string SanitizeQueueName(string value)
    {
        string lower = new(value.ToLowerInvariant().Where(c => char.IsLetterOrDigit(c)).ToArray());
        return string.IsNullOrEmpty(lower) ? "type" : lower;
    }

    private static string TruncateTableName(string value)
    {
        return value.Length <= 63 ? value : value[..63];
    }

    private static string TruncateQueueName(string value)
    {
        string trimmed = value.Trim('-');
        if (trimmed.Length <= 63)
        {
            return trimmed;
        }

        return trimmed[..63].TrimEnd('-');
    }

    private static string ShortHash(string value)
    {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(hash[..4]);
    }
}

internal sealed class AzureStorageJsonSerializer
{
    private readonly JsonSerializerOptions options;

    public AzureStorageJsonSerializer(AzureStorageOptions storageOptions)
    {
        ArgumentNullException.ThrowIfNull(storageOptions);
        options = new JsonSerializerOptions(storageOptions.SerializerOptions);
    }

    public byte[] SerializeToBytes<TValue>(TValue value) => JsonSerializer.SerializeToUtf8Bytes(value, options);

    public string SerializeToString<TValue>(TValue value) => JsonSerializer.Serialize(value, options);

    public T? Deserialize<T>(string json) => JsonSerializer.Deserialize<T>(json, options);

    public T? Deserialize<T>(ReadOnlySpan<byte> json) => JsonSerializer.Deserialize<T>(json, options);

    public Task SerializeAsync<TValue>(Stream stream, TValue value, CancellationToken cancellationToken)
    {
        return JsonSerializer.SerializeAsync(stream, value, options, cancellationToken);
    }

    public async Task<T?> DeserializeAsync<T>(Stream stream, CancellationToken cancellationToken)
    {
        return await JsonSerializer.DeserializeAsync<T>(stream, options, cancellationToken).ConfigureAwait(false);
    }
}

internal static class AzureStorageExceptionHelper
{
    internal const string DataPropertyName = "Data";
    internal const string MetadataSchemaVersionKey = "schemaversion";
    internal const string MetadataChecksumKey = "checksum";

    internal static bool IsNotFound(RequestFailedException exception) => exception.Status == 404;

    internal static bool IsConflictOrPrecondition(RequestFailedException exception) => exception.Status is 409 or 412;
}
