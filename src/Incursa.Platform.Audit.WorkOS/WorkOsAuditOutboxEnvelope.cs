namespace Incursa.Platform.Audit.WorkOS;

public sealed record WorkOsAuditOutboxEnvelope(
    string EventId,
    DateTimeOffset OccurredAtUtc,
    string Action,
    string DisplayMessage,
    string Outcome,
    string? DataJson,
    string? ActorType,
    string? ActorId,
    string? ActorDisplay,
    int? Version,
    IReadOnlyList<WorkOsAuditOutboxAnchor> Anchors,
    WorkOsAuditOutboxCorrelation? Correlation);
