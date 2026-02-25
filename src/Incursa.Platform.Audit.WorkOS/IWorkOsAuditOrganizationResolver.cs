namespace Incursa.Platform.Audit.WorkOS;

using Incursa.Platform.Audit;

public interface IWorkOsAuditOrganizationResolver
{
    ValueTask<string?> ResolveOrganizationIdAsync(string tenantId, AuditEvent auditEvent, CancellationToken cancellationToken);
}
