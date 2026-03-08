using Incursa.Platform.Health;

namespace Incursa.Platform.HealthProbe;

/// <summary>
/// HTTP configuration for health probe execution.
/// </summary>
public sealed class HealthProbeHttpOptions
{
    /// <summary>
    /// Gets or sets the base URL for HTTP probe execution.
    /// </summary>
    public Uri? BaseUrl { get; set; }

    /// <summary>
    /// Gets or sets the relative or absolute live endpoint path.
    /// </summary>
    public string LivePath { get; set; } = PlatformHealthEndpoints.Live;

    /// <summary>
    /// Gets or sets the relative or absolute ready endpoint path.
    /// </summary>
    public string ReadyPath { get; set; } = PlatformHealthEndpoints.Ready;

    /// <summary>
    /// Gets or sets the relative or absolute dependency endpoint path.
    /// </summary>
    public string DepPath { get; set; } = PlatformHealthEndpoints.Dep;

    /// <summary>
    /// Gets or sets the API key value sent with probe requests.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Gets or sets the header name used when sending <see cref="ApiKey"/>.
    /// </summary>
    public string ApiKeyHeaderName { get; set; } = HealthProbeDefaults.DefaultApiKeyHeaderName;

    /// <summary>
    /// Gets or sets a value indicating whether server certificate validation is disabled.
    /// </summary>
    public bool AllowInsecureTls { get; set; }

    internal HealthProbeHttpOptions Clone()
    {
        return new HealthProbeHttpOptions
        {
            BaseUrl = BaseUrl,
            LivePath = LivePath,
            ReadyPath = ReadyPath,
            DepPath = DepPath,
            ApiKey = ApiKey,
            ApiKeyHeaderName = ApiKeyHeaderName,
            AllowInsecureTls = AllowInsecureTls,
        };
    }
}
