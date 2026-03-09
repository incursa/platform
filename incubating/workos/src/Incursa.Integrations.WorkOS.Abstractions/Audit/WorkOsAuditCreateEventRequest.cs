namespace Incursa.Integrations.WorkOS.Abstractions.Audit;

public sealed record WorkOsAuditCreateEventRequest(
    string OrganizationId,
    string Action,
    DateTimeOffset OccurredAtUtc,
    WorkOsAuditActor Actor,
    IReadOnlyList<WorkOsAuditTarget> Targets,
    WorkOsAuditContext? Context = null,
    IReadOnlyDictionary<string, string>? Metadata = null,
    int? Version = null,
    string? IdempotencyKey = null);
