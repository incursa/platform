namespace Incursa.Platform.HealthProbe;

/// <summary>
/// Configuration options for health probe execution.
/// </summary>
public sealed class HealthProbeOptions
{
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

    internal HealthProbeOptions Clone()
    {
        return new HealthProbeOptions
        {
            DefaultBucket = DefaultBucket,
            Timeout = Timeout,
            IncludeData = IncludeData,
        };
    }
}
