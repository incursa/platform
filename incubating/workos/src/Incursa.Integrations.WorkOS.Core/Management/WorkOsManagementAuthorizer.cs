namespace Incursa.Integrations.WorkOS.Core.Management;

using Incursa.Integrations.WorkOS.Abstractions.Management;
using Incursa.Integrations.WorkOS.Core.Clients;

public sealed class WorkOsManagementAuthorizer : IWorkOsManagementAuthorizer
{
    private readonly IWorkOsManagementClient _client;

    public WorkOsManagementAuthorizer(IWorkOsManagementClient client)
    {
        ArgumentNullException.ThrowIfNull(client);
        _client = client;
    }

    public ValueTask<bool> EnsureOrgAdminAsync(string organizationId, string subject, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(organizationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);
        return _client.IsOrganizationAdminAsync(organizationId, subject, ct);
    }
}

