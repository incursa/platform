namespace Incursa.Platform.HealthProbe;

/// <summary>
/// Describes a resolved health probe request.
/// </summary>
/// <param name="Bucket">The selected health bucket.</param>
/// <param name="IncludeData">Whether payload data should be included in output.</param>
public sealed record HealthProbeRequest(string Bucket, bool IncludeData);
