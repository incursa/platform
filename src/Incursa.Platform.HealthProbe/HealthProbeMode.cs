namespace Incursa.Platform.HealthProbe;

/// <summary>
/// Defines how health probes are executed.
/// </summary>
public enum HealthProbeMode
{
    /// <summary>
    /// Execute health checks in-process from the current host service provider.
    /// </summary>
    InProcess = 0,

    /// <summary>
    /// Execute health checks over HTTP against a configured endpoint.
    /// </summary>
    Http = 1,
}
