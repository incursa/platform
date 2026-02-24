using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Incursa.Platform.Health;

public static class PlatformHealthServiceCollectionExtensions
{
    public static IHealthChecksBuilder AddPlatformHealthChecks(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddStartupLatch();

        return services
            .AddHealthChecks()
            .AddCheck(
                PlatformHealthConstants.SelfCheckName,
                static () => HealthCheckResult.Healthy("Process is running"),
                tags: [PlatformHealthTags.Live])
            .AddReadyCheck<StartupLatchHealthCheck>(PlatformHealthConstants.StartupLatchCheckName);
    }
}
