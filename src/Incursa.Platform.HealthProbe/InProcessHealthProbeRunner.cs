using Incursa.Platform.Health;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Incursa.Platform.HealthProbe;

public sealed class InProcessHealthProbeRunner : IHealthProbeRunner
{
    private readonly HealthCheckService healthCheckService;

    public InProcessHealthProbeRunner(HealthCheckService healthCheckService)
    {
        this.healthCheckService = healthCheckService ?? throw new ArgumentNullException(nameof(healthCheckService));
    }

    public async Task<HealthProbeResult> RunAsync(HealthProbeRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var bucket = request.Bucket.ToLowerInvariant() switch
        {
            PlatformHealthTags.Live => PlatformHealthBucket.Live,
            PlatformHealthTags.Ready => PlatformHealthBucket.Ready,
            PlatformHealthTags.Dep => PlatformHealthBucket.Dep,
            _ => throw new HealthProbeArgumentException($"Unknown bucket '{request.Bucket}'."),
        };

        var startedAt = TimeProvider.System.GetTimestamp();
        var report = await PlatformHealthRunner.RunAsync(healthCheckService, bucket, cancellationToken).ConfigureAwait(false);
        var duration = TimeProvider.System.GetElapsedTime(startedAt);
        var payload = PlatformHealthReportFormatter.Format(
            bucket,
            report,
            new PlatformHealthDataOptions
            {
                IncludeData = request.IncludeData,
            });

        var exitCode = report.Status == HealthStatus.Healthy
            ? HealthProbeExitCodes.Healthy
            : HealthProbeExitCodes.NonHealthy;

        return new HealthProbeResult(
            request.Bucket,
            report.Status.ToString(),
            exitCode,
            payload,
            duration);
    }
}
