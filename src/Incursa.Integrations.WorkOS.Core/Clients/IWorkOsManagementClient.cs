namespace Incursa.Integrations.WorkOS.Core.Clients;

using Incursa.Integrations.WorkOS.Abstractions.Management;

public sealed record WorkOsManagedApiKey(
    string ApiKeyId,
    string OrganizationId,
    string DisplayName,
    DateTimeOffset CreatedUtc,
    DateTimeOffset? ExpiresUtc,
    DateTimeOffset? RevokedUtc,
    IReadOnlyCollection<string> Permissions);

public interface IWorkOsManagementClient
{
    ValueTask<WorkOsManagedApiKey?> ValidateApiKeyAsync(string presentedKey, CancellationToken ct = default);

    ValueTask<WorkOsCreatedApiKey> CreateApiKeyAsync(string organizationId, string displayName, IReadOnlyCollection<string> scopes, int? ttlHours, CancellationToken ct = default);

    IAsyncEnumerable<WorkOsApiKeySummary> ListApiKeysAsync(string organizationId, CancellationToken ct = default);

    ValueTask<WorkOsApiKeySummary?> GetApiKeyAsync(string organizationId, string apiKeyId, CancellationToken ct = default);

    ValueTask RevokeApiKeyAsync(string organizationId, string apiKeyId, CancellationToken ct = default);

    ValueTask<bool> IsOrganizationAdminAsync(string organizationId, string subject, CancellationToken ct = default);
}

