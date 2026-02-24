using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Incursa.Platform.HealthProbe;

/// <summary>
/// Extensions for registering health probe services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers health probe services in the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddIncursaHealthProbe(
        this IServiceCollection services,
        Action<HealthProbeOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<HealthProbeOptions>();
        services.AddSingleton<IConfigureOptions<HealthProbeOptions>>(static serviceProvider =>
        {
            return new HealthProbeOptionsConfigurator(serviceProvider.GetService<IConfiguration>());
        });

        if (configure is not null)
        {
            services.Configure(configure);
        }

        services.AddTransient<IHealthProbeRunner, InProcessHealthProbeRunner>();

        return services;
    }

}
