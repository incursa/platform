namespace Incursa.Integrations.WorkOS.Abstractions.Telemetry;

using Incursa.Integrations.WorkOS.Abstractions.Authentication;

public interface IWorkOsIntegrationTelemetry
{
    void ApiKeyValidation(string? organizationId, string? tenantId, WorkOsValidationErrorCode code, bool cacheHit);

    void ScopeAuthorization(string organizationId, string tenantId, string scope, bool allowed);

    void ApiKeyLifecycle(string operation, string organizationId, string apiKeyId, bool success);

    void WebhookProcessed(string eventType, string eventId, bool processed, bool duplicate, string? failureReason);
}

