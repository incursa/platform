namespace Incursa.Integrations.WorkOS.Core.Clients;

using System.Net.Http.Headers;
using Incursa.Integrations.WorkOS.Abstractions.Configuration;
using Incursa.Integrations.WorkOS.Abstractions.Management;
using Incursa.Integrations.WorkOS.Abstractions.Mapping;

public sealed class WorkOsManagementHttpClient : IWorkOsManagementClient
{
    private readonly HttpClient _httpClient;
    private readonly IWorkOsTenantMapper _tenantMapper;

    public WorkOsManagementHttpClient(HttpClient httpClient, WorkOsManagementOptions options, IWorkOsTenantMapper tenantMapper)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(tenantMapper);

        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/", UriKind.Absolute);
        _httpClient.Timeout = options.RequestTimeout;
        if (!string.IsNullOrWhiteSpace(options.ApiKey))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);
        }

        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _tenantMapper = tenantMapper;
    }

    public async ValueTask<WorkOsManagedApiKey?> ValidateApiKeyAsync(string presentedKey, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(presentedKey))
        {
            return null;
        }

        var response = await PostJsonAsync("api_keys/validations", new Dictionary<string, object?>
        {
            ["value"] = presentedKey,
        }, ct).ConfigureAwait(false);

        if (response is null)
        {
            return null;
        }

        if (!response.RootElement.TryGetProperty("api_key", out var apiKeyElement))
        {
            return null;
        }

        if (apiKeyElement.ValueKind == JsonValueKind.Null || apiKeyElement.ValueKind == JsonValueKind.Undefined)
        {
            return null;
        }

        if (apiKeyElement.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var keyId = TryGetString(apiKeyElement, "id") ?? TryGetString(apiKeyElement, "api_key_id") ?? string.Empty;
        var orgId = TryGetString(apiKeyElement, "organization_id")
            ?? TryGetOwnerOrganizationId(apiKeyElement)
            ?? string.Empty;
        var displayName = TryGetString(apiKeyElement, "name") ?? "api-key";

        List<string> permissions = [];
        if (apiKeyElement.TryGetProperty("permissions", out var permissionsElement) && permissionsElement.ValueKind == JsonValueKind.Array)
        {
            permissions.AddRange(permissionsElement.EnumerateArray().Where(static x => x.ValueKind == JsonValueKind.String).Select(static x => x.GetString() ?? string.Empty).Where(static x => !string.IsNullOrWhiteSpace(x)));
        }

        return new WorkOsManagedApiKey(
            ApiKeyId: keyId,
            OrganizationId: orgId,
            DisplayName: displayName,
            CreatedUtc: TryGetDateTimeOffset(apiKeyElement, "created_at") ?? DateTimeOffset.UtcNow,
            ExpiresUtc: TryGetDateTimeOffset(apiKeyElement, "expires_at"),
            RevokedUtc: TryGetDateTimeOffset(apiKeyElement, "revoked_at"),
            Permissions: permissions);
    }

    public async ValueTask<WorkOsCreatedApiKey> CreateApiKeyAsync(string organizationId, string displayName, IReadOnlyCollection<string> scopes, int? ttlHours, CancellationToken ct = default)
    {
        var response = await PostJsonAsync($"organizations/{Uri.EscapeDataString(organizationId)}/api_keys", new Dictionary<string, object?>
        {
            ["name"] = displayName,
            ["permissions"] = scopes,
            ["ttl_hours"] = ttlHours,
        }, ct).ConfigureAwait(false);

        if (response is null)
        {
            throw new InvalidOperationException("WorkOS did not return api key payload.");
        }

        var payload = UnwrapData(response.RootElement);
        var tenantId = await _tenantMapper.GetTenantIdAsync(organizationId, ct).ConfigureAwait(false) ?? string.Empty;
        return new WorkOsCreatedApiKey(
            ApiKeyId: TryGetFirstString(payload, "id", "api_key_id") ?? string.Empty,
            DisplayName: TryGetString(payload, "name") ?? displayName,
            CreatedUtc: TryGetDateTimeOffset(payload, "created_at") ?? DateTimeOffset.UtcNow,
            ExpiresUtc: TryGetDateTimeOffset(payload, "expires_at"),
            EffectiveScopes: scopes.Select(static x => x.ToLowerInvariant()).Distinct(StringComparer.Ordinal).OrderBy(static x => x, StringComparer.Ordinal).ToArray(),
            OrganizationId: organizationId,
            TenantId: tenantId,
            Secret: TryGetCreateSecret(payload) ?? string.Empty);
    }

    public async IAsyncEnumerable<WorkOsApiKeySummary> ListApiKeysAsync(string organizationId, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        using var response = await _httpClient.GetAsync($"organizations/{Uri.EscapeDataString(organizationId)}/api_keys", ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            yield break;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        using var payload = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);
        if (!payload.RootElement.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array)
        {
            yield break;
        }

        var tenantId = await _tenantMapper.GetTenantIdAsync(organizationId, ct).ConfigureAwait(false) ?? string.Empty;
        foreach (var item in data.EnumerateArray())
        {
            yield return ParseSummary(item, organizationId, tenantId);
        }
    }

    public async ValueTask<WorkOsApiKeySummary?> GetApiKeyAsync(string organizationId, string apiKeyId, CancellationToken ct = default)
    {
        using var response = await _httpClient.GetAsync($"organizations/{Uri.EscapeDataString(organizationId)}/api_keys/{Uri.EscapeDataString(apiKeyId)}", ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        using var payload = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);
        var tenantId = await _tenantMapper.GetTenantIdAsync(organizationId, ct).ConfigureAwait(false) ?? string.Empty;
        return ParseSummary(payload.RootElement, organizationId, tenantId);
    }

    public async ValueTask RevokeApiKeyAsync(string organizationId, string apiKeyId, CancellationToken ct = default)
    {
        using var response = await _httpClient.PostAsync($"organizations/{Uri.EscapeDataString(organizationId)}/api_keys/{Uri.EscapeDataString(apiKeyId)}/revoke", content: null, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    public async ValueTask<bool> IsOrganizationAdminAsync(string organizationId, string subject, CancellationToken ct = default)
    {
        using var response = await _httpClient.GetAsync($"user_management/organization_memberships?organization_id={Uri.EscapeDataString(organizationId)}&user_id={Uri.EscapeDataString(subject)}", ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            return false;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        using var payload = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);
        if (!payload.RootElement.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        foreach (var item in data.EnumerateArray())
        {
            var role = TryGetString(item, "role_slug")
                ?? (item.TryGetProperty("role", out var roleObj) ? TryGetString(roleObj, "slug") : null);

            if (string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase) || string.Equals(role, "owner", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private async ValueTask<JsonDocument?> PostJsonAsync(string path, object body, CancellationToken ct)
    {
        using var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        using var response = await _httpClient.PostAsync(path, content, ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        return await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);
    }

    private static WorkOsApiKeySummary ParseSummary(JsonElement item, string organizationId, string tenantId)
    {
        List<string> scopes = [];
        if (item.TryGetProperty("permissions", out var permissions) && permissions.ValueKind == JsonValueKind.Array)
        {
            scopes.AddRange(permissions.EnumerateArray()
                .Where(static x => x.ValueKind == JsonValueKind.String)
                .Select(static x => (x.GetString() ?? string.Empty).ToLowerInvariant())
                .Where(static x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(static x => x, StringComparer.Ordinal));
        }

        return new WorkOsApiKeySummary(
            ApiKeyId: TryGetString(item, "id") ?? string.Empty,
            DisplayName: TryGetString(item, "name") ?? "api-key",
            CreatedUtc: TryGetDateTimeOffset(item, "created_at") ?? DateTimeOffset.UtcNow,
            ExpiresUtc: TryGetDateTimeOffset(item, "expires_at"),
            RevokedUtc: TryGetDateTimeOffset(item, "revoked_at"),
            EffectiveScopes: scopes,
            OrganizationId: organizationId,
            TenantId: tenantId);
    }

    private static JsonElement UnwrapData(JsonElement root)
    {
        if (root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Object)
        {
            return data;
        }

        return root;
    }

    private static string? TryGetCreateSecret(JsonElement payload)
    {
        return TryGetFirstString(payload, "secret", "value", "api_key", "token", "one_time_token_secret", "one_time_secret")
            ?? TryGetNestedString(payload, "one_time_token", "secret", "token", "value")
            ?? TryGetNestedString(payload, "token", "secret", "value")
            ?? TryGetNestedString(payload, "api_key", "secret", "value", "token", "api_key")
            ?? TryGetNestedString(payload, "data", "secret", "value", "token");
    }

    private static string? TryGetNestedString(JsonElement element, string objectName, params string[] propertyNames)
    {
        if (element.TryGetProperty(objectName, out var nested) && nested.ValueKind == JsonValueKind.Object)
        {
            return TryGetFirstString(nested, propertyNames);
        }

        return null;
    }

    private static string? TryGetFirstString(JsonElement element, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            var value = TryGetString(element, propertyName);
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

    private static string? TryGetString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    private static string? TryGetOwnerOrganizationId(JsonElement element)
    {
        if (!element.TryGetProperty("owner", out var ownerElement) || ownerElement.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var ownerType = TryGetString(ownerElement, "type");
        if (!string.Equals(ownerType, "organization", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return TryGetString(ownerElement, "id");
    }

    private static DateTimeOffset? TryGetDateTimeOffset(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.String && DateTimeOffset.TryParse(value.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed))
        {
            return parsed;
        }

        return null;
    }
}

