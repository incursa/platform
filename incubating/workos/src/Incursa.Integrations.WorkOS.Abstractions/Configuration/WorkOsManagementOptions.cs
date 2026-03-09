namespace Incursa.Integrations.WorkOS.Abstractions.Configuration;

public sealed class WorkOsManagementOptions
{
    public string BaseUrl { get; set; } = "https://api.workos.com";

    public string ApiKey { get; set; } = string.Empty;

    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(5);

    public int RetryCount { get; set; } = 2;
}
