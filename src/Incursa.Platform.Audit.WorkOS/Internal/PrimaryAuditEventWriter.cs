namespace Incursa.Platform.Audit.WorkOS.Internal;

using Incursa.Platform.Audit;

internal sealed class PrimaryAuditEventWriter : IPrimaryAuditEventWriter
{
    private readonly IAuditEventWriter inner;

    public PrimaryAuditEventWriter(IAuditEventWriter inner)
    {
        this.inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    public Task WriteAsync(AuditEvent auditEvent, CancellationToken cancellationToken)
    {
        return inner.WriteAsync(auditEvent, cancellationToken);
    }
}
