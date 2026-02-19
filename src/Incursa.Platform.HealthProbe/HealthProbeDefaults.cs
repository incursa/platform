namespace Incursa.Platform.HealthProbe;

internal static class HealthProbeDefaults
{
    public const string ConfigurationRootKey = "Incursa:HealthProbe";
    public const string DefaultApiKeyHeaderName = "X-Api-Key";
    public const string HttpClientName = "Incursa.Platform.HealthProbe";
    public const string HttpClientInsecureName = "Incursa.Platform.HealthProbe.Insecure";
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(2);
}
