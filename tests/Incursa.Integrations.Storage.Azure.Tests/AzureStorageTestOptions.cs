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

namespace Incursa.Integrations.Storage.Azure.Tests;

internal static class AzureStorageTestOptions
{
    private const string DevelopmentStorageConnectionString = "UseDevelopmentStorage=true";

    public static AzureStorageOptions CreateUnitOptions(Action<AzureStorageOptions>? configure = null)
    {
        AzureStorageOptions options = CreateOptions(DevelopmentStorageConnectionString, createResourcesIfMissing: false, nameof(CreateUnitOptions));
        configure?.Invoke(options);
        return options;
    }

    public static AzureStorageOptions CreateIntegrationOptions(
        string connectionString,
        string scenarioName,
        Action<AzureStorageOptions>? configure = null)
    {
        AzureStorageOptions options = CreateOptions(connectionString, createResourcesIfMissing: true, scenarioName);
        configure?.Invoke(options);
        return options;
    }

    private static AzureStorageOptions CreateOptions(
        string connectionString,
        bool createResourcesIfMissing,
        string scenarioName)
    {
        string token = Guid.NewGuid().ToString("N")[..12];
        string label = SanitizeLowercase(scenarioName, fallback: "case", maxLength: 10);

        return new AzureStorageOptions
        {
            ConnectionString = connectionString,
            CreateResourcesIfMissing = createResourcesIfMissing,
            RecordTablePrefix = $"Rec{token[..6].ToUpperInvariant()}",
            LookupTablePrefix = $"Lkp{token[6..].ToUpperInvariant()}",
            PayloadContainerName = $"payload-{label}-{token}",
            WorkPayloadContainerName = $"workpayload-{label}-{token}",
            WorkQueuePrefix = $"work-{label}-{token[..6]}",
            CoordinationContainerName = $"coord-{label}-{token}",
            CoordinationTableName = $"Coord{token.ToUpperInvariant()}",
        };
    }

    private static string SanitizeLowercase(string value, string fallback, int maxLength)
    {
        string sanitized = new(value.ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());
        if (string.IsNullOrEmpty(sanitized))
        {
            sanitized = fallback;
        }

        return sanitized.Length <= maxLength ? sanitized : sanitized[..maxLength];
    }
}
