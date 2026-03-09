namespace Incursa.Integrations.WorkOS.Abstractions.Configuration;

public sealed class WorkOsUserProfileHydrationOptions
{
    public bool Enabled { get; set; } = true;

    public TimeSpan CacheTtl { get; set; } = TimeSpan.FromMinutes(5);

    public bool HydrateOnSignIn { get; set; } = true;

    public bool RevalidateOnRequestIfStale { get; set; } = true;

    public string ClaimPrefix { get; set; } = "workos:profile:";

    public bool IncludeRawProfileJson { get; set; }
}
