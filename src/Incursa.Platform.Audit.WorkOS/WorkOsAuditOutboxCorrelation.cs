namespace Incursa.Platform.Audit.WorkOS;

public sealed record WorkOsAuditOutboxCorrelation(
    string CorrelationId,
    string? CausationId,
    string? TraceId,
    string? SpanId,
    IReadOnlyDictionary<string, string>? Tags);
