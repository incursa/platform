namespace Incursa.Integrations.WorkOS.Webhooks.Internal;

using System.Globalization;
using System.Text.Json;
using Incursa.Platform.Webhooks;

internal sealed class WorkOsWebhookClassifier : IWebhookClassifier
{
    public Task<ClassifyResult> ClassifyAsync(WebhookEnvelope envelope, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            using var payload = JsonDocument.Parse(envelope.BodyBytes);
            var root = payload.RootElement;
            var providerEventId = TryGetString(root, "id");
            var eventType = TryGetString(root, "event") ?? TryGetString(root, "type");
            if (string.IsNullOrWhiteSpace(eventType))
            {
                return Task.FromResult(new ClassifyResult(
                    WebhookIngestDecision.Rejected,
                    providerEventId,
                    null,
                    providerEventId,
                    TryGetString(root, "organization_id"),
                    null,
                    "missing_event_type"));
            }

            var organizationId = TryGetString(root, "organization_id");
            var createdAtUtc = TryGetDateTimeOffset(root, "created_at");
            var summary = JsonSerializer.Serialize(new WorkOsWebhookSummary(
                providerEventId,
                eventType,
                organizationId,
                createdAtUtc));

            return Task.FromResult(new ClassifyResult(
                WebhookIngestDecision.Accepted,
                providerEventId,
                eventType,
                providerEventId,
                organizationId,
                summary,
                null));
        }
        catch (JsonException)
        {
            return Task.FromResult(new ClassifyResult(
                WebhookIngestDecision.Rejected,
                null,
                null,
                null,
                null,
                null,
                "invalid_json"));
        }
    }

    private static string? TryGetString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    private static DateTimeOffset? TryGetDateTimeOffset(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value) || value.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return DateTimeOffset.TryParse(value.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed)
            ? parsed
            : null;
    }

    private sealed record WorkOsWebhookSummary(
        string? ProviderEventId,
        string EventType,
        string? OrganizationId,
        DateTimeOffset? CreatedAtUtc);
}
