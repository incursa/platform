namespace Incursa.Integrations.WorkOS.Abstractions.Webhooks;

public sealed record WorkOsWebhookProcessResult(bool Processed, bool Duplicate, string? Message = null);

