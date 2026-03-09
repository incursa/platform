namespace Incursa.Integrations.WorkOS.Core.Authorization;

using Incursa.Integrations.WorkOS.Abstractions.Authentication;
using Incursa.Integrations.WorkOS.Abstractions.Authorization;
using Incursa.Integrations.WorkOS.Abstractions.Telemetry;

public sealed class WorkOsScopeAuthorizer : IWorkOsScopeAuthorizer
{
    private readonly IWorkOsIntegrationTelemetry _telemetry;

    public WorkOsScopeAuthorizer(IWorkOsIntegrationTelemetry? telemetry = null)
    {
        _telemetry = telemetry ?? NullWorkOsIntegrationTelemetry.Instance;
    }

    public ValueTask<WorkOsApiKeyValidationResult> AuthorizeAsync(
        WorkOsAuthIdentity identity,
        string requiredScope,
        string tenantId,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentException.ThrowIfNullOrWhiteSpace(requiredScope);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        if (!string.Equals(identity.TenantId, tenantId, StringComparison.Ordinal))
        {
            _telemetry.ScopeAuthorization(identity.OrganizationId, tenantId, requiredScope, false);
            return ValueTask.FromResult(WorkOsApiKeyValidationResult.Failure(WorkOsValidationErrorCode.UnknownOrganization, "unknown_org"));
        }

        var granted = identity.Scopes.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var allowed = IsScopeAuthorized(requiredScope, granted);
        _telemetry.ScopeAuthorization(identity.OrganizationId, tenantId, requiredScope, allowed);

        return ValueTask.FromResult(allowed
            ? WorkOsApiKeyValidationResult.Success(identity)
            : WorkOsApiKeyValidationResult.Failure(WorkOsValidationErrorCode.InsufficientScope, "insufficient_scope"));
    }

    private static bool IsScopeAuthorized(string requiredScope, HashSet<string> grantedScopes)
    {
        if (grantedScopes.Contains(requiredScope))
        {
            return true;
        }

        if (requiredScope.Equals("artifacts:nuget:read", StringComparison.OrdinalIgnoreCase))
        {
            return grantedScopes.Contains("nuget.read")
                || HasAdminRead(grantedScopes);
        }

        if (requiredScope.Equals("artifacts:nuget:push", StringComparison.OrdinalIgnoreCase))
        {
            return grantedScopes.Contains("nuget.push")
                || HasAdminWrite(grantedScopes);
        }

        if (requiredScope.Equals("artifacts:apt:read", StringComparison.OrdinalIgnoreCase))
        {
            return grantedScopes.Contains("apt.read")
                || HasAdminRead(grantedScopes);
        }

        if (requiredScope.Equals("artifacts:apt:push", StringComparison.OrdinalIgnoreCase))
        {
            return grantedScopes.Contains("apt.push")
                || HasAdminWrite(grantedScopes);
        }

        if (requiredScope.Equals("artifacts:raw:read", StringComparison.OrdinalIgnoreCase))
        {
            return grantedScopes.Contains("raw.read")
                || HasAdminRead(grantedScopes);
        }

        if (requiredScope.Equals("artifacts:raw:push", StringComparison.OrdinalIgnoreCase))
        {
            return grantedScopes.Contains("raw.push")
                || HasAdminWrite(grantedScopes);
        }

        if (requiredScope.Equals("artifacts:admin:read", StringComparison.OrdinalIgnoreCase))
        {
            return HasAdminRead(grantedScopes);
        }

        if (requiredScope.Equals("artifacts:admin:write", StringComparison.OrdinalIgnoreCase))
        {
            return HasAdminWrite(grantedScopes);
        }

        return HasAdminWrite(grantedScopes);
    }

    private static bool HasAdminRead(HashSet<string> grantedScopes)
        => grantedScopes.Contains("artifacts:admin:read")
           || HasAdminWrite(grantedScopes);

    private static bool HasAdminWrite(HashSet<string> grantedScopes)
        => grantedScopes.Contains("artifacts:admin:write")
           || grantedScopes.Contains("admin");
}

