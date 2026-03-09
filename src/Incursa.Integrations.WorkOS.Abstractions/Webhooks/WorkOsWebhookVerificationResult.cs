namespace Incursa.Integrations.WorkOS.Abstractions.Webhooks;

public sealed record WorkOsWebhookVerificationResult(bool IsValid, string? FailureReason);

