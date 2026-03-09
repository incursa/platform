namespace Incursa.Integrations.Cloudflare.Options;

public sealed class CloudflareCustomHostnameOptions
{
    public const string SectionName = "Cloudflare:CustomHostnames";

    public string? ZoneId { get; set; }
}
