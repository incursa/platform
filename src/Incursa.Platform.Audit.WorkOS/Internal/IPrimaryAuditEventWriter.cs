namespace Incursa.Platform.Audit.WorkOS.Internal;

using Incursa.Platform.Audit;

internal interface IPrimaryAuditEventWriter
{
    Task WriteAsync(AuditEvent auditEvent, CancellationToken cancellationToken);
}
