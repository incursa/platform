namespace Incursa.Integrations.WorkOS.Core.Authentication;

using Incursa.Integrations.WorkOS.Abstractions.Authentication;
using Incursa.Integrations.WorkOS.Abstractions.Configuration;
using Incursa.Integrations.WorkOS.Abstractions.Mapping;
using Incursa.Integrations.WorkOS.Abstractions.Telemetry;
using Incursa.Integrations.WorkOS.Core.Authorization;
using Incursa.Integrations.WorkOS.Core.Clients;
using Microsoft.Extensions.Caching.Memory;

public sealed class WorkOsApiKeyAuthenticator : IWorkOsApiKeyAuthenticator
{
    private readonly IWorkOsManagementClient _managementClient;
    private readonly IWorkOsTenantMapper _tenantMapper;
    private readonly WorkOsPermissionMapper _permissionMapper;
    private readonly IMemoryCache _cache;
    private readonly WorkOsIntegrationOptions _options;
    private readonly IWorkOsIntegrationTelemetry _telemetry;

    public WorkOsApiKeyAuthenticator(
        IWorkOsManagementClient managementClient,
        IWorkOsTenantMapper tenantMapper,
        WorkOsPermissionMapper permissionMapper,
        IMemoryCache cache,
        WorkOsIntegrationOptions options,
        IWorkOsIntegrationTelemetry? telemetry = null)
    {
        ArgumentNullException.ThrowIfNull(managementClient);
        ArgumentNullException.ThrowIfNull(tenantMapper);
        ArgumentNullException.ThrowIfNull(permissionMapper);
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(options);

        _managementClient = managementClient;
        _tenantMapper = tenantMapper;
        _permissionMapper = permissionMapper;
        _cache = cache;
        _options = options;
        _telemetry = telemetry ?? NullWorkOsIntegrationTelemetry.Instance;
    }

    public async ValueTask<WorkOsApiKeyValidationResult> ValidateApiKeyAsync(string presentedKey, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(presentedKey))
        {
            return WorkOsApiKeyValidationResult.Failure(WorkOsValidationErrorCode.Invalid, "invalid");
        }

        var cacheKey = "workos:key:validation:" + ComputeKeyFingerprint(presentedKey);
        if (_cache.TryGetValue<WorkOsApiKeyValidationResult>(cacheKey, out var cached) && cached is not null)
        {
            _telemetry.ApiKeyValidation(cached.Identity?.OrganizationId, cached.Identity?.TenantId, cached.ErrorCode, true);
            return cached;
        }

        try
        {
            var managedKey = await _managementClient.ValidateApiKeyAsync(presentedKey, ct).ConfigureAwait(false);
            var result = await BuildResultAsync(managedKey, ct).ConfigureAwait(false);
            _cache.Set(cacheKey, result, _options.CacheTtl);
            _telemetry.ApiKeyValidation(result.Identity?.OrganizationId, result.Identity?.TenantId, result.ErrorCode, false);
            return result;
        }
        catch
        {
            if (_options.StaleReadGracePeriod > TimeSpan.Zero && _cache.TryGetValue<WorkOsApiKeyValidationResult>(cacheKey, out var stale) && stale is not null)
            {
                _telemetry.ApiKeyValidation(stale.Identity?.OrganizationId, stale.Identity?.TenantId, stale.ErrorCode, true);
                return stale;
            }

            return WorkOsApiKeyValidationResult.Failure(WorkOsValidationErrorCode.InternalError, "invalid");
        }
    }

    private async ValueTask<WorkOsApiKeyValidationResult> BuildResultAsync(WorkOsManagedApiKey? managedKey, CancellationToken ct)
    {
        if (managedKey is null)
        {
            return WorkOsApiKeyValidationResult.Failure(WorkOsValidationErrorCode.Invalid, "invalid");
        }

        if (managedKey.RevokedUtc is not null)
        {
            return WorkOsApiKeyValidationResult.Failure(WorkOsValidationErrorCode.Revoked, "revoked");
        }

        if (managedKey.ExpiresUtc is not null && managedKey.ExpiresUtc.Value <= DateTimeOffset.UtcNow)
        {
            return WorkOsApiKeyValidationResult.Failure(WorkOsValidationErrorCode.Expired, "expired");
        }

        var tenantId = await _tenantMapper.GetTenantIdAsync(managedKey.OrganizationId, ct).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            return WorkOsApiKeyValidationResult.Failure(WorkOsValidationErrorCode.UnknownOrganization, "unknown_org");
        }

        var scopes = _permissionMapper.MapToScopes(managedKey.Permissions, _options.StrictPermissionMapping, out var unknown);
        if (_options.StrictPermissionMapping && unknown.Count > 0)
        {
            return WorkOsApiKeyValidationResult.Failure(WorkOsValidationErrorCode.InsufficientScope, "insufficient_scope");
        }

        var identity = new WorkOsAuthIdentity(
            Subject: "api_key:" + managedKey.ApiKeyId,
            ApiKeyId: managedKey.ApiKeyId,
            OrganizationId: managedKey.OrganizationId,
            TenantId: tenantId,
            Scopes: scopes,
            CreatedUtc: managedKey.CreatedUtc,
            ExpiresUtc: managedKey.ExpiresUtc,
            RevokedUtc: managedKey.RevokedUtc,
            DisplayName: managedKey.DisplayName);

        return WorkOsApiKeyValidationResult.Success(identity);
    }

    private static string ComputeKeyFingerprint(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input.Trim());
        var hash = System.Security.Cryptography.SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }
}

