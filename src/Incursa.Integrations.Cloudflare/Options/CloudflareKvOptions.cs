namespace Incursa.Integrations.Cloudflare.Options;

public sealed class CloudflareKvOptions
{
    public const string SectionName = "Cloudflare:KV";

    public string? AccountId { get; set; }

    public string? NamespaceId { get; set; }

    /// <summary>
    /// Gets or sets the maximum duration allowed for a KV key listing operation before timing out.
    /// Set to 0 or less to disable the operation-level timeout.
    /// </summary>
    public int ListOperationTimeoutSeconds { get; set; } = 5;
}
