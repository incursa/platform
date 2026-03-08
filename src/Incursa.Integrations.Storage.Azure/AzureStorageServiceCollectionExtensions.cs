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

using Incursa.Platform.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Incursa.Integrations.Storage.Azure;

/// <summary>
/// Registers the Azure-backed storage provider.
/// </summary>
public static class AzureStorageServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Azure-backed storage provider using an options callback.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">The provider configuration callback.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAzureStorage(
        this IServiceCollection services,
        Action<AzureStorageOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        AzureStorageOptions options = new();
        configure(options);
        return services.AddAzureStorage(options);
    }

    /// <summary>
    /// Registers the Azure-backed storage provider using a connection string.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">The Azure Storage connection string.</param>
    /// <param name="configure">Optional provider configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAzureStorage(
        this IServiceCollection services,
        string connectionString,
        Action<AzureStorageOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Connection string is required.", nameof(connectionString));
        }

        AzureStorageOptions options = new()
        {
            ConnectionString = connectionString,
        };

        configure?.Invoke(options);
        return services.AddAzureStorage(options);
    }

    /// <summary>
    /// Registers the Azure-backed storage provider using the supplied options.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="options">The provider options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAzureStorage(
        this IServiceCollection services,
        AzureStorageOptions options)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);

        AzureStorageOptions snapshot = options.Clone();
        AzureStorageOptionsValidator validator = new();
        OptionsValidationHelper.ValidateAndThrow(snapshot, validator);

        services.AddOptions<AzureStorageOptions>().ValidateOnStart();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<AzureStorageOptions>>(validator));
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IConfigureOptions<AzureStorageOptions>>(
                new ConfigureNamedOptions<AzureStorageOptions>(Options.DefaultName, target => target.CopyFrom(snapshot))));

        services.TryAddSingleton(sp => sp.GetRequiredService<IOptions<AzureStorageOptions>>().Value);
        services.TryAddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.TryAddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.TryAddSingleton(TimeProvider.System);

        services.TryAddSingleton<AzureStorageClientFactory>();
        services.TryAddSingleton<AzureStorageNameResolver>();
        services.TryAddSingleton<AzureStorageJsonSerializer>();
        services.TryAddSingleton<IPayloadStore, AzureBlobPayloadStore>();
        services.TryAddSingleton<ICoordinationStore, AzureCoordinationStore>();
        services.TryAddSingleton(typeof(IRecordStore<>), typeof(AzureTableRecordStore<>));
        services.TryAddSingleton(typeof(ILookupStore<>), typeof(AzureTableLookupStore<>));
        services.TryAddSingleton(typeof(IWorkStore<>), typeof(AzureQueueWorkStore<>));

        return services;
    }
}
