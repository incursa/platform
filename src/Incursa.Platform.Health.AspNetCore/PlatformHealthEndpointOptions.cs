namespace Incursa.Platform.Health.AspNetCore;

public sealed class PlatformHealthEndpointOptions
{
    public bool RequireAuthorization { get; set; }

    public string? AuthorizationPolicy { get; set; }

    public bool IncludeData { get; set; }
}
