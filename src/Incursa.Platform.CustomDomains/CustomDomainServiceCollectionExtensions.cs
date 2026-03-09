namespace Incursa.Platform.CustomDomains;

using Incursa.Platform.CustomDomains.Internal;
using Microsoft.Extensions.DependencyInjection;

public static class CustomDomainServiceCollectionExtensions
{
    public static IServiceCollection AddCustomDomains(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<CustomDomainStorageContext>();
        services.AddSingleton<ICustomDomainAdministrationService, StorageBackedCustomDomainAdministrationService>();
        services.AddSingleton<ICustomDomainQueryService, StorageBackedCustomDomainQueryService>();

        return services;
    }
}
