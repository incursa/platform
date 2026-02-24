namespace Incursa.Platform.Health;

public static class PlatformHealthConstants
{
    public const string SelfCheckName = "self";
    public const string StartupLatchCheckName = "startup_latch";
    public static readonly TimeSpan DefaultDependencySuccessCacheTtl = TimeSpan.FromMinutes(2);
    public static readonly TimeSpan DefaultDependencyFailureCacheTtl = TimeSpan.FromSeconds(45);
}
