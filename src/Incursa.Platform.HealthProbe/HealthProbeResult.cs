using Incursa.Platform.Health;

namespace Incursa.Platform.HealthProbe;

/// <summary>
/// Represents the outcome of a health probe.
/// </summary>
public sealed class HealthProbeResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HealthProbeResult"/> class.
    /// </summary>
    /// <param name="bucket">Executed health bucket.</param>
    /// <param name="status">Health status.</param>
    /// <param name="exitCode">Process exit code to return to the caller.</param>
    /// <param name="payload">Health report payload.</param>
    /// <param name="duration">Elapsed probe duration.</param>
    public HealthProbeResult(
        string bucket,
        string status,
        int exitCode,
        PlatformHealthReportPayload payload,
        TimeSpan duration)
    {
        Bucket = bucket;
        Status = status;
        ExitCode = exitCode;
        Payload = payload;
        Duration = duration;
    }

    /// <summary>
    /// Gets the executed bucket.
    /// </summary>
    public string Bucket { get; }

    /// <summary>
    /// Gets the probe status.
    /// </summary>
    public string Status { get; }

    /// <summary>
    /// Gets the exit code for the probe.
    /// </summary>
    public int ExitCode { get; }

    /// <summary>
    /// Gets a value indicating whether the probe was healthy.
    /// </summary>
    public bool IsHealthy => string.Equals(Status, "Healthy", StringComparison.Ordinal);

    /// <summary>
    /// Gets the duration of the probe.
    /// </summary>
    public TimeSpan Duration { get; }

    /// <summary>
    /// Gets the standardized health payload.
    /// </summary>
    public PlatformHealthReportPayload Payload { get; }
}
