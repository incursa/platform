namespace Incursa.Integrations.WorkOS.Abstractions.Configuration;

public sealed class WorkOsClientCredentialsOptions
{
    public bool Enabled { get; set; }

    public string Authority { get; set; } = string.Empty;

    public string ClientId { get; set; } = string.Empty;

    public string ClientSecret { get; set; } = string.Empty;

    public string Scope { get; set; } = string.Empty;

    public string TokenEndpointPath { get; set; } = "oauth2/token";

    public TimeSpan TokenFetchTimeout { get; set; } = TimeSpan.FromSeconds(5);

    public TimeSpan TokenMinRefreshBeforeExpiry { get; set; } = TimeSpan.FromSeconds(30);

    public int RetryCount { get; set; } = 3;
}
