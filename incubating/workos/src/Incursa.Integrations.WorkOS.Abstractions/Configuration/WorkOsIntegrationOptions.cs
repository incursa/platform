namespace Incursa.Integrations.WorkOS.Abstractions.Configuration;

public sealed class WorkOsIntegrationOptions
{
    public string BaseUrl { get; set; } = "https://api.workos.com";

    public string ApiKey { get; set; } = string.Empty;

    public string WebhookSigningSecret { get; set; } = string.Empty;

    public bool StrictPermissionMapping { get; set; } = true;

    public TimeSpan CacheTtl { get; set; } = TimeSpan.FromSeconds(45);

    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(5);

    public int RetryCount { get; set; } = 2;

    public TimeSpan StaleReadGracePeriod { get; set; } = TimeSpan.Zero;
}

