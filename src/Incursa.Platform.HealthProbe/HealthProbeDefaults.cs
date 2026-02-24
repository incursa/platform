namespace Incursa.Platform.HealthProbe;

internal static class HealthProbeDefaults
{
    public const string ConfigurationRootKey = "Incursa:HealthProbe";
    public const string CommandName = "health";
    public const string DefaultBucket = "ready";
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(2);
}
