namespace Incursa.Integrations.Cloudflare.Options;

public sealed class CloudflareApiOptions
{
    public const string SectionName = "Cloudflare";

    public Uri BaseUrl { get; set; } = new("https://api.cloudflare.com/client/v4", UriKind.Absolute);

    public string? ApiToken { get; set; }

    public string? AccountId { get; set; }

    public string? ZoneId { get; set; }

    public int RequestTimeoutSeconds { get; set; } = 8;

    public int RetryCount { get; set; } = 2;

    public bool ForceIpv4 { get; set; } = true;
}
