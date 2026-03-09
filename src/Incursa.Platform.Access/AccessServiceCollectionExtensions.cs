namespace Incursa.Platform.Access;

using Incursa.Platform.Access.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

public static class AccessServiceCollectionExtensions
{
    public static IServiceCollection AddAccess(
        this IServiceCollection services,
        Action<AccessRegistryBuilder> configureRegistry)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureRegistry);

        var builder = new AccessRegistryBuilder();
        configureRegistry(builder);
        return services.AddAccess(builder.Build("default"));
    }

    public static IServiceCollection AddAccess(this IServiceCollection services, IAccessRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(registry);

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton(registry);
        services.AddSingleton<IAccessRegistry>(registry);
        services.AddSingleton<AccessStorageContext>();
        services.AddSingleton<IAccessAuditJournal, StorageBackedAccessAuditJournal>();
        services.AddSingleton<IAccessQueryService, StorageBackedAccessQueryService>();
        services.AddSingleton<IEffectiveAccessEvaluator, DefaultEffectiveAccessEvaluator>();
        services.AddSingleton<IAccessAdministrationService, StorageBackedAccessAdministrationService>();

        return services;
    }
}
