namespace Incursa.Integrations.WorkOS.Audit;

public sealed record WorkOsAuditOutboxCorrelation(
    string CorrelationId,
    string? CausationId,
    string? TraceId,
    string? SpanId,
    IReadOnlyDictionary<string, string>? Tags);
