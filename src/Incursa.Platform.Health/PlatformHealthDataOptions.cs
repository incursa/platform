namespace Incursa.Platform.Health;

public sealed class PlatformHealthDataOptions
{
    public bool IncludeData { get; set; }

    public ISet<string> SensitiveKeyFragments { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "secret",
        "token",
        "password",
        "apikey",
        "api_key",
        "key",
        "connectionstring",
        "connection_string",
    };
}
