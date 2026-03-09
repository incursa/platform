namespace Incursa.Integrations.WorkOS.Abstractions.Authorization;

using Incursa.Integrations.WorkOS.Abstractions.Authentication;

public interface IWorkOsScopeAuthorizer
{
    ValueTask<WorkOsApiKeyValidationResult> AuthorizeAsync(
        WorkOsAuthIdentity identity,
        string requiredScope,
        string tenantId,
        CancellationToken ct = default);
}

