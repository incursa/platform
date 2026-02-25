namespace Incursa.Platform.Audit.WorkOS;

using Incursa.Platform.Audit;

public interface IAuditOutboxSinkSerializer
{
    string SinkName { get; }

    ValueTask<AuditOutboxSinkMessage?> SerializeAsync(AuditEvent auditEvent, CancellationToken cancellationToken);
}
