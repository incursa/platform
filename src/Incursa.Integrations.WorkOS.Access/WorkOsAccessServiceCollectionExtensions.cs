namespace Incursa.Integrations.WorkOS.Access;

using Incursa.Platform.Access;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

public static class WorkOsAccessServiceCollectionExtensions
{
    public static IServiceCollection AddWorkOsAccess(
        this IServiceCollection services,
        Action<WorkOsAccessOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton(TimeProvider.System);

        if (configure is not null)
        {
            services.Configure(configure);
        }
        else
        {
            services.TryAddSingleton(Options.Create(new WorkOsAccessOptions()));
        }

        services.TryAddSingleton<IWorkOsAccessSynchronizationService, WorkOsAccessSynchronizationService>();
        return services;
    }
}
