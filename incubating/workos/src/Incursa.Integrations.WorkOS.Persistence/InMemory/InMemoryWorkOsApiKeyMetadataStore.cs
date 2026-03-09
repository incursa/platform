namespace Incursa.Integrations.WorkOS.Persistence.InMemory;

using Incursa.Integrations.WorkOS.Abstractions.Management;
using Incursa.Integrations.WorkOS.Abstractions.Persistence;

public sealed class InMemoryWorkOsApiKeyMetadataStore : IWorkOsApiKeyMetadataStore
{
    private readonly Dictionary<(string OrgId, string KeyId), WorkOsApiKeySummary> _items = [];
    private readonly Lock _gate = new();

    public ValueTask UpsertAsync(WorkOsApiKeySummary summary, CancellationToken ct = default)
    {
        lock (_gate)
        {
            _items[(summary.OrganizationId, summary.ApiKeyId)] = summary;
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask<WorkOsApiKeySummary?> GetAsync(string organizationId, string apiKeyId, CancellationToken ct = default)
    {
        lock (_gate)
        {
            return ValueTask.FromResult(_items.TryGetValue((organizationId, apiKeyId), out var value) ? value : null);
        }
    }

    public async IAsyncEnumerable<WorkOsApiKeySummary> ListAsync(string organizationId, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        WorkOsApiKeySummary[] items;
        lock (_gate)
        {
            items = _items.Values.Where(x => string.Equals(x.OrganizationId, organizationId, StringComparison.Ordinal)).OrderByDescending(static x => x.CreatedUtc).ToArray();
        }

        foreach (var item in items)
        {
            ct.ThrowIfCancellationRequested();
            yield return item;
            await Task.Yield();
        }
    }
}

