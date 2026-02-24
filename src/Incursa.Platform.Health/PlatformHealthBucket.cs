namespace Incursa.Platform.Health;

[Flags]
public enum PlatformHealthBucket
{
    None = 0,
    Live = 1,
    Ready = 2,
    Dep = 4,
}
