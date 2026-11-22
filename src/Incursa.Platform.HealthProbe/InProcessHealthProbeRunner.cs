using Incursa.Platform.Health;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Incursa.Platform.HealthProbe;

/// <summary>
/// Executes health probes directly through the registered health check service.
/// </summary>
public sealed class InProcessHealthProbeRunner : IHealthProbeRunner
{
    private readonly HealthCheckService healthCheckService;

    /// <summary>
    /// Initializes a new instance of the <see cref="InProcessHealthProbeRunner"/> class.
    /// </summary>
    /// <param name="healthCheckService">The registered health check service.</param>
    public InProcessHealthProbeRunner(HealthCheckService healthCheckService)
    {
        this.healthCheckService = healthCheckService ?? throw new ArgumentNullException(nameof(healthCheckService));
    }

    /// <inheritdoc />
    public async Task<HealthProbeResult> RunAsync(HealthProbeRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var bucket = request.Bucket switch
        {
            var value when value.Equals(PlatformHealthTags.Live, StringComparison.OrdinalIgnoreCase) => PlatformHealthBucket.Live,
            var value when value.Equals(PlatformHealthTags.Ready, StringComparison.OrdinalIgnoreCase) => PlatformHealthBucket.Ready,
            var value when value.Equals(PlatformHealthTags.Dep, StringComparison.OrdinalIgnoreCase) => PlatformHealthBucket.Dep,
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
