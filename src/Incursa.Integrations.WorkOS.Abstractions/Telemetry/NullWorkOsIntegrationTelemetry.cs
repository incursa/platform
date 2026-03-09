namespace Incursa.Integrations.WorkOS.Abstractions.Telemetry;

using Incursa.Integrations.WorkOS.Abstractions.Authentication;

public sealed class NullWorkOsIntegrationTelemetry : IWorkOsIntegrationTelemetry
{
    public static readonly NullWorkOsIntegrationTelemetry Instance = new();

    private NullWorkOsIntegrationTelemetry()
    {
    }

    public void ApiKeyValidation(string? organizationId, string? tenantId, WorkOsValidationErrorCode code, bool cacheHit)
    {
    }

    public void ScopeAuthorization(string organizationId, string tenantId, string scope, bool allowed)
    {
    }

    public void ApiKeyLifecycle(string operation, string organizationId, string apiKeyId, bool success)
    {
    }

    public void WebhookProcessed(string eventType, string eventId, bool processed, bool duplicate, string? failureReason)
    {
    }
}

