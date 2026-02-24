using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Incursa.Platform.Health;

public sealed class CachedHealthCheckOptions
{
    public TimeSpan HealthyCacheDuration { get; set; } = TimeSpan.FromSeconds(30);

    public TimeSpan DegradedCacheDuration { get; set; } = TimeSpan.FromSeconds(10);

    public TimeSpan UnhealthyCacheDuration { get; set; } = TimeSpan.FromSeconds(5);

    public TimeSpan GetCacheDuration(HealthStatus status)
    {
        return status switch
        {
            HealthStatus.Healthy => HealthyCacheDuration,
            HealthStatus.Degraded => DegradedCacheDuration,
            _ => UnhealthyCacheDuration,
        };
    }
}
