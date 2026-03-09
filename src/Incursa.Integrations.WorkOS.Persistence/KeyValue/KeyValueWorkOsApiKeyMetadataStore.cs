namespace Incursa.Integrations.WorkOS.Persistence.KeyValue;

using Incursa.Integrations.WorkOS.Abstractions.Management;
using Incursa.Integrations.WorkOS.Abstractions.Persistence;

public sealed class KeyValueWorkOsApiKeyMetadataStore : IWorkOsApiKeyMetadataStore
{
    private readonly IWorkOsKeyValueStore _store;

    public KeyValueWorkOsApiKeyMetadataStore(IWorkOsKeyValueStore store)
    {
        ArgumentNullException.ThrowIfNull(store);
        _store = store;
    }

    public ValueTask UpsertAsync(WorkOsApiKeySummary summary, CancellationToken ct = default)
    {
        return _store.SetStringAsync(BuildKey(summary.OrganizationId, summary.ApiKeyId), JsonSerializer.Serialize(summary), null, ct);
    }

    public async ValueTask<WorkOsApiKeySummary?> GetAsync(string organizationId, string apiKeyId, CancellationToken ct = default)
    {
        var raw = await _store.GetStringAsync(BuildKey(organizationId, apiKeyId), ct).ConfigureAwait(false);
        return string.IsNullOrWhiteSpace(raw) ? null : JsonSerializer.Deserialize<WorkOsApiKeySummary>(raw);
    }

    public async IAsyncEnumerable<WorkOsApiKeySummary> ListAsync(string organizationId, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        var prefix = BuildPrefix(organizationId);
        await foreach (var item in _store.EnumerateByPrefixAsync(prefix, ct).ConfigureAwait(false))
        {
            var parsed = JsonSerializer.Deserialize<WorkOsApiKeySummary>(item.Value);
            if (parsed is not null)
            {
                yield return parsed;
            }
        }
    }

    private static string BuildPrefix(string orgId) => "workos:keys:" + orgId + ":";

    private static string BuildKey(string orgId, string apiKeyId) => BuildPrefix(orgId) + apiKeyId;
}

