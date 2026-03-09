namespace Incursa.Integrations.WorkOS.Persistence.DependencyInjection;

using Incursa.Integrations.WorkOS.Abstractions.Persistence;
using Incursa.Integrations.WorkOS.Persistence.InMemory;
using Incursa.Integrations.WorkOS.Persistence.KeyValue;
using Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWorkOsInMemoryPersistence(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IWorkOsOrgTenantMappingStore, InMemoryWorkOsOrgTenantMappingStore>();
        services.AddSingleton<IWorkOsApiKeyMetadataStore, InMemoryWorkOsApiKeyMetadataStore>();
        services.AddSingleton<IWorkOsWebhookEventDedupStore, InMemoryWorkOsWebhookEventDedupStore>();
        return services;
    }

    public static IServiceCollection AddWorkOsKeyValuePersistence(this IServiceCollection services, Func<IServiceProvider, IWorkOsKeyValueStore> storeFactory)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(storeFactory);

        services.AddSingleton<IWorkOsKeyValueStore>(storeFactory);
        services.AddSingleton<IWorkOsOrgTenantMappingStore, KeyValueWorkOsOrgTenantMappingStore>();
        services.AddSingleton<IWorkOsApiKeyMetadataStore, KeyValueWorkOsApiKeyMetadataStore>();
        services.AddSingleton<IWorkOsWebhookEventDedupStore, KeyValueWorkOsWebhookEventDedupStore>();
        return services;
    }
}

