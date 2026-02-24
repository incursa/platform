using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Incursa.Platform.Health;

public sealed class StartupLatchHealthCheck : IHealthCheck
{
    private readonly IStartupLatch latch;

    public StartupLatchHealthCheck(IStartupLatch latch)
    {
        this.latch = latch ?? throw new ArgumentNullException(nameof(latch));
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(latch.IsReady
            ? HealthCheckResult.Healthy("Startup complete")
            : HealthCheckResult.Unhealthy("Starting"));
    }
}
