namespace Incursa.Integrations.WorkOS.Audit;

public sealed record AuditOutboxSinkMessage(
    string Topic,
    string Payload,
    string? CorrelationId = null);
