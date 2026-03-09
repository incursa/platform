namespace Incursa.Integrations.WorkOS.Abstractions.Audit;

public sealed record WorkOsAuditTarget(
    string Id,
    string Type,
    string? Name = null,
    IReadOnlyDictionary<string, string>? Metadata = null);
