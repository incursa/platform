namespace Incursa.Integrations.WorkOS.Abstractions.Persistence;

using Incursa.Integrations.WorkOS.Abstractions.Management;

public interface IWorkOsApiKeyMetadataStore
{
    ValueTask UpsertAsync(WorkOsApiKeySummary summary, CancellationToken ct = default);

    ValueTask<WorkOsApiKeySummary?> GetAsync(string organizationId, string apiKeyId, CancellationToken ct = default);

    IAsyncEnumerable<WorkOsApiKeySummary> ListAsync(string organizationId, CancellationToken ct = default);
}

