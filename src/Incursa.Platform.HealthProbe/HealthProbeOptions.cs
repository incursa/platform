namespace Incursa.Platform.HealthProbe;

/// <summary>
/// Configuration options for health probe execution.
/// </summary>
public sealed class HealthProbeOptions
{
    /// <summary>
    /// Gets or sets the probe execution mode.
    /// </summary>
    public HealthProbeMode Mode { get; set; } = HealthProbeMode.InProcess;

    /// <summary>
    /// Gets or sets the default bucket when no explicit bucket is provided.
    /// </summary>
    public string DefaultBucket { get; set; } = HealthProbeDefaults.DefaultBucket;

    /// <summary>
    /// Gets or sets the health probe timeout.
    /// </summary>
    public TimeSpan Timeout { get; set; } = HealthProbeDefaults.DefaultTimeout;

    /// <summary>
    /// Gets or sets a value indicating whether JSON output includes check data.
    /// </summary>
    public bool IncludeData { get; set; }

    /// <summary>
    /// Gets or sets HTTP probe options.
    /// </summary>
    public HealthProbeHttpOptions Http { get; set; } = new();

    internal HealthProbeOptions Clone()
    {
        return new HealthProbeOptions
        {
            Mode = Mode,
            DefaultBucket = DefaultBucket,
            Timeout = Timeout,
            IncludeData = IncludeData,
            Http = Http.Clone(),
        };
    }
}
