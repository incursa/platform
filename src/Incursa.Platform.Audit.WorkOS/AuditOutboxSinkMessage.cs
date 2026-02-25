namespace Incursa.Platform.Audit.WorkOS;

public sealed record AuditOutboxSinkMessage(
    string Topic,
    string Payload,
    string? CorrelationId = null);
