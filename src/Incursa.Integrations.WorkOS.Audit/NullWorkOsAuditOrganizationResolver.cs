namespace Incursa.Integrations.WorkOS.Audit;

using Incursa.Platform.Audit;

public sealed class NullWorkOsAuditOrganizationResolver : IWorkOsAuditOrganizationResolver
{
    public static readonly NullWorkOsAuditOrganizationResolver Instance = new();

    private NullWorkOsAuditOrganizationResolver()
    {
    }

    public ValueTask<string?> ResolveOrganizationIdAsync(string tenantId, AuditEvent auditEvent, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult<string?>(null);
    }
}
