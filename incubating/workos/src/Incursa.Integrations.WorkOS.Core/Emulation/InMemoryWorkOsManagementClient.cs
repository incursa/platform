namespace Incursa.Integrations.WorkOS.Core.Emulation;

using Incursa.Integrations.WorkOS.Abstractions.Management;
using Incursa.Integrations.WorkOS.Core.Clients;

public sealed class InMemoryWorkOsManagementClient : IWorkOsManagementClient
{
    private readonly InMemoryWorkOsState state;

    public InMemoryWorkOsManagementClient(InMemoryWorkOsState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        this.state = state;
    }

    public ValueTask<WorkOsManagedApiKey?> ValidateApiKeyAsync(string presentedKey, CancellationToken ct = default)
        => ValueTask.FromResult(state.ValidateApiKey(presentedKey));

    public ValueTask<WorkOsCreatedApiKey> CreateApiKeyAsync(string organizationId, string displayName, IReadOnlyCollection<string> scopes, int? ttlHours, CancellationToken ct = default)
        => ValueTask.FromResult(state.CreateApiKey(organizationId, displayName, scopes, ttlHours));

    public async IAsyncEnumerable<WorkOsApiKeySummary> ListApiKeysAsync(string organizationId, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        IReadOnlyCollection<WorkOsApiKeySummary> summaries = state.ListApiKeys(organizationId);
        foreach (WorkOsApiKeySummary summary in summaries)
        {
            ct.ThrowIfCancellationRequested();
            yield return summary;
            await Task.Yield();
        }
    }

    public ValueTask<WorkOsApiKeySummary?> GetApiKeyAsync(string organizationId, string apiKeyId, CancellationToken ct = default)
        => ValueTask.FromResult(state.GetApiKey(organizationId, apiKeyId));

    public ValueTask RevokeApiKeyAsync(string organizationId, string apiKeyId, CancellationToken ct = default)
    {
        _ = state.RevokeApiKey(organizationId, apiKeyId);
        return ValueTask.CompletedTask;
    }

    public ValueTask<bool> IsOrganizationAdminAsync(string organizationId, string subject, CancellationToken ct = default)
        => ValueTask.FromResult(state.IsOrganizationAdmin(organizationId, subject));
}
