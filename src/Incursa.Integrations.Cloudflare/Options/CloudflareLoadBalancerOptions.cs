namespace Incursa.Integrations.Cloudflare.Options;

public sealed class CloudflareLoadBalancerOptions
{
    public const string SectionName = "Cloudflare:LoadBalancing";

    public string? AccountId { get; set; }

    public string? ZoneId { get; set; }
}
