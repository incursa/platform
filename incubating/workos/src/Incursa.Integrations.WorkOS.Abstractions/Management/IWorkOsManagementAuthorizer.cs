namespace Incursa.Integrations.WorkOS.Abstractions.Management;

public interface IWorkOsManagementAuthorizer
{
    ValueTask<bool> EnsureOrgAdminAsync(string organizationId, string subject, CancellationToken ct = default);
}

