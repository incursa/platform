namespace Incursa.Integrations.ElectronicNotary.Proof.AspNetCore;

using System.Text.Json;
using Incursa.Platform.Webhooks;

internal sealed class ProofWebhookClassifier : IWebhookClassifier
{
    private const string EventPropertyName = "event";
    private const string EventIdPropertyName = "id";
    private const string TransactionIdPropertyName = "transaction_id";
    private const string DateOccurredPropertyName = "date_occurred";

    public Task<ClassifyResult> ClassifyAsync(WebhookEnvelope envelope, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        try
        {
            using JsonDocument jsonDocument = JsonDocument.Parse(envelope.BodyBytes);
            JsonElement root = jsonDocument.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return Task.FromResult(Reject("Webhook payload root must be a JSON object."));
            }

            string? eventName = TryReadString(root, EventPropertyName);
            if (string.IsNullOrWhiteSpace(eventName))
            {
                return Task.FromResult(Reject("Webhook payload does not contain an event name."));
            }

            string? providerEventId = TryReadString(root, EventIdPropertyName);
            JsonElement dataElement = TryReadDataObject(root);
            string? transactionId = TryReadString(root, TransactionIdPropertyName) ?? TryReadString(dataElement, TransactionIdPropertyName);
            string? dateOccurred = TryReadString(root, DateOccurredPropertyName) ?? TryReadString(dataElement, DateOccurredPropertyName);
            string dedupeKey = CreateDedupeKey(envelope.Provider, eventName, transactionId, dateOccurred, providerEventId, envelope.BodyBytes);
            string parsedSummary = BuildParsedSummary(eventName, transactionId, dateOccurred);

            return Task.FromResult(
                new ClassifyResult(
                    WebhookIngestDecision.Accepted,
                    providerEventId,
                    eventName,
                    dedupeKey,
                    null,
                    parsedSummary,
                    null));
        }
        catch (JsonException)
        {
            return Task.FromResult(Reject("Webhook payload is not valid JSON."));
        }
    }

    private static ClassifyResult Reject(string reason)
    {
        return new ClassifyResult(
            WebhookIngestDecision.Rejected,
            null,
            null,
            null,
            null,
            null,
            reason);
    }

    private static string CreateDedupeKey(
        string provider,
        string eventName,
        string? transactionId,
        string? dateOccurred,
        string? providerEventId,
        byte[] bodyBytes)
    {
        if (!string.IsNullOrWhiteSpace(transactionId) && !string.IsNullOrWhiteSpace(dateOccurred))
        {
            return $"{provider}:{eventName}:{transactionId}:{dateOccurred}";
        }

        return WebhookDedupe.Create(provider, providerEventId, bodyBytes).Key;
    }

    private static string BuildParsedSummary(string eventName, string? transactionId, string? dateOccurred)
    {
        return JsonSerializer.Serialize(
            new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                [EventPropertyName] = eventName,
                [TransactionIdPropertyName] = transactionId,
                [DateOccurredPropertyName] = dateOccurred,
            });
    }

    private static string? TryReadString(JsonElement root, string propertyName)
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (!root.TryGetProperty(propertyName, out JsonElement propertyValue))
        {
            return null;
        }

        if (propertyValue.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return propertyValue.GetString();
    }

    private static JsonElement TryReadDataObject(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("data", out JsonElement dataElement))
        {
            return dataElement;
        }

        return default;
    }
}
