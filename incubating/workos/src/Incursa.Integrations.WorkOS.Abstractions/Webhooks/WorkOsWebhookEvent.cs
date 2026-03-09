namespace Incursa.Integrations.WorkOS.Abstractions.Webhooks;

public sealed record WorkOsWebhookEvent(
    string EventId,
    string EventType,
    string? OrganizationId,
    DateTimeOffset OccurredUtc,
    JsonDocument Payload);

