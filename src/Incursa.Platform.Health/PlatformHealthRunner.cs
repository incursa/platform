using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Incursa.Platform.Health;

public static class PlatformHealthRunner
{
    public static Task<HealthReport> RunAsync(
        HealthCheckService healthCheckService,
        PlatformHealthBucket bucket,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(healthCheckService);
        var tag = PlatformHealthReportFormatter.BucketToTag(bucket);
        return healthCheckService.CheckHealthAsync(
            registration => registration.Tags.Contains(tag, StringComparer.Ordinal),
            cancellationToken);
    }
}
