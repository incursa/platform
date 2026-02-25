namespace Incursa.Platform.Audit.WorkOS;

public sealed class WorkOsAuditSinkOptions
{
    public string ApiBaseUrl { get; set; } = "https://api.workos.com";

    public string ApiKey { get; set; } = string.Empty;

    public string OutboxTopic { get; set; } = "audit.sink.workos.v1";

    public string DefaultActorType { get; set; } = "system";

    public bool IncludeCorrelationMetadata { get; set; } = true;

    public bool IncludeDisplayMessageMetadata { get; set; } = true;

    public bool UseEventIdAsIdempotencyKey { get; set; } = true;

    public ISet<string> OrganizationAnchorTypes { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "organization",
        "workosorganization",
    };

    public ISet<string> TenantAnchorTypes { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "tenant",
    };

    public IDictionary<string, int> ActionVersions { get; } = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
}
