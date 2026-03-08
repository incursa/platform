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

using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;

namespace Incursa.Integrations.Storage.Azure.Tests;

internal sealed class AzuriteTestEnvironment
{
    private const string ConnectionStringEnvironmentVariable = "INCURSA_AZURE_STORAGE_CONNECTION_STRING";
    private const string EnableTablesEnvironmentVariable = "INCURSA_AZURE_STORAGE_ENABLE_TABLES";

    private AzuriteTestEnvironment(string connectionString, bool tablesEnabled)
    {
        ConnectionString = connectionString;
        TablesEnabled = tablesEnabled;
    }

    public string ConnectionString { get; }

    public bool TablesEnabled { get; }

    public static async Task<AzuriteTestEnvironment> GetBlobAndQueueAsync()
    {
        string? configuredConnectionString = Environment.GetEnvironmentVariable(ConnectionStringEnvironmentVariable);
        Assert.SkipWhen(
            string.IsNullOrWhiteSpace(configuredConnectionString),
            $"Set {ConnectionStringEnvironmentVariable} to an Azurite connection string to run Azure storage integration tests.");

        string connectionString = configuredConnectionString!;
        try
        {
            await new BlobServiceClient(connectionString)
                .GetPropertiesAsync(TestContext.Current.CancellationToken)
                .ConfigureAwait(false);
            await new QueueServiceClient(connectionString)
                .GetPropertiesAsync(TestContext.Current.CancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            Assert.Skip(
                $"Azurite blob/queue services are not available using {ConnectionStringEnvironmentVariable}: {exception.GetBaseException().Message}");
        }

        return new AzuriteTestEnvironment(connectionString, GetTablesEnabled());
    }

    public static async Task<AzuriteTestEnvironment> GetTableAsync()
    {
        AzuriteTestEnvironment environment = await GetBlobAndQueueAsync().ConfigureAwait(false);
        Assert.SkipUnless(
            environment.TablesEnabled,
            $"Set {EnableTablesEnvironmentVariable}=true when Azurite table support is available.");

        string probeTableName = $"Probe{Guid.NewGuid():N}"[..17];
        try
        {
            TableClient tableClient = new TableServiceClient(environment.ConnectionString).GetTableClient(probeTableName);
            await tableClient.CreateIfNotExistsAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);
            await tableClient.DeleteAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            Assert.Skip(
                $"Azurite table support is not available using {ConnectionStringEnvironmentVariable}: {exception.GetBaseException().Message}");
        }

        return environment;
    }

    private static bool GetTablesEnabled()
    {
        string? configuredValue = Environment.GetEnvironmentVariable(EnableTablesEnvironmentVariable);
        return configuredValue is not null &&
               (configuredValue.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                configuredValue.Equals("1", StringComparison.OrdinalIgnoreCase) ||
                configuredValue.Equals("yes", StringComparison.OrdinalIgnoreCase));
    }
}
