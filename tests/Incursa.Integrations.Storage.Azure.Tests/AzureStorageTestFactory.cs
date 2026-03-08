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

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Incursa.Integrations.Storage.Azure.Tests;

internal static class AzureStorageTestFactory
{
    public static ServiceProvider BuildServiceProvider(AzureStorageOptions options, TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(options);

        ServiceCollection services = new();
        services.AddLogging();
        if (timeProvider is not null)
        {
            services.AddSingleton(timeProvider);
        }

        services.AddAzureStorage(options);
        return services.BuildServiceProvider(validateScopes: true);
    }

    public static AzureTableRecordStore<TRecord> CreateRecordStore<TRecord>(AzureStorageOptions? options = null)
    {
        AzureStorageOptions effectiveOptions = options ?? AzureStorageTestOptions.CreateUnitOptions();
        return new AzureTableRecordStore<TRecord>(
            new AzureStorageClientFactory(effectiveOptions),
            new AzureStorageNameResolver(effectiveOptions),
            effectiveOptions,
            new AzureStorageJsonSerializer(effectiveOptions),
            NullLogger<AzureTableRecordStore<TRecord>>.Instance);
    }

    public static AzureCoordinationStore CreateCoordinationStore(AzureStorageOptions? options = null, TimeProvider? timeProvider = null)
    {
        AzureStorageOptions effectiveOptions = options ?? AzureStorageTestOptions.CreateUnitOptions();
        return new AzureCoordinationStore(
            new AzureStorageClientFactory(effectiveOptions),
            effectiveOptions,
            new AzureStorageNameResolver(effectiveOptions),
            new AzureStorageJsonSerializer(effectiveOptions),
            timeProvider ?? TimeProvider.System,
            NullLogger<AzureCoordinationStore>.Instance);
    }
}
