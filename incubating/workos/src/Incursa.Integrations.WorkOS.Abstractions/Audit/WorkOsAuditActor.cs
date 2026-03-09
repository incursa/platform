namespace Incursa.Integrations.WorkOS.Abstractions.Audit;

public sealed record WorkOsAuditActor(
    string Id,
    string Type,
    string? Name = null,
    IReadOnlyDictionary<string, string>? Metadata = null);
