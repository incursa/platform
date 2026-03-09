namespace Incursa.Integrations.WorkOS.Audit;

using Incursa.Platform.Audit;

public interface IWorkOsAuditOrganizationResolver
{
    ValueTask<string?> ResolveOrganizationIdAsync(string tenantId, AuditEvent auditEvent, CancellationToken cancellationToken);
}
