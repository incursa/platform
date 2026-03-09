namespace Incursa.Integrations.ElectronicNotary.Proof.Contracts;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Represents a flexible webhook envelope for events without a dedicated typed payload.
/// </summary>
public sealed record ProofWebhookEnvelope
{
    /// <summary>
    /// Gets the webhook event name.
    /// </summary>
    [JsonPropertyName("event")]
    required public string Event { get; init; }

    /// <summary>
    /// Gets the event payload data.
    /// </summary>
    [JsonPropertyName("data")]
    public JsonElement Data { get; init; }
}
