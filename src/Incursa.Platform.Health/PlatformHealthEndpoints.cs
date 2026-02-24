namespace Incursa.Platform.Health;

public static class PlatformHealthEndpoints
{
    public const string Live = "/healthz";
    public const string Ready = "/readyz";
    public const string Dep = "/depz";
}
