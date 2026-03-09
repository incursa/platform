namespace Incursa.Integrations.WorkOS.Audit.Internal;

using Incursa.Platform.Audit;

internal interface IPrimaryAuditEventWriter
{
    Task WriteAsync(AuditEvent auditEvent, CancellationToken cancellationToken);
}
