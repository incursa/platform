namespace Incursa.Integrations.Cloudflare.Options;

public sealed class CloudflareR2Options
{
    public const string SectionName = "Cloudflare:R2";

    public string? Endpoint { get; set; }

    public string? AccessKeyId { get; set; }

    public string? SecretAccessKey { get; set; }

    public string? Bucket { get; set; }

    public string Region { get; set; } = "auto";
}
