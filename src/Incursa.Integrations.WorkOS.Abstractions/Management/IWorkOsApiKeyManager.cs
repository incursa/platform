namespace Incursa.Integrations.WorkOS.Abstractions.Management;

public interface IWorkOsApiKeyManager
{
    ValueTask<WorkOsCreatedApiKey> CreateAsync(
        string organizationId,
        string displayName,
        IReadOnlyCollection<string> scopes,
        int? ttlHours,
        CancellationToken ct = default);

    IAsyncEnumerable<WorkOsApiKeySummary> ListAsync(string organizationId, CancellationToken ct = default);

    ValueTask<WorkOsApiKeySummary?> GetAsync(string organizationId, string apiKeyId, CancellationToken ct = default);

    ValueTask RevokeAsync(string organizationId, string apiKeyId, CancellationToken ct = default);
}

