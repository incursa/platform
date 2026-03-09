namespace Incursa.Integrations.ElectronicNotary.Proof.Contracts;

using System.Text.Json.Serialization;
using Incursa.Integrations.ElectronicNotary.Proof.Types;

/// <summary>
/// Typed payload for the <c>transaction.released</c> Proof webhook event.
/// </summary>
public sealed record TransactionReleasedEvent
{
    /// <summary>
    /// Gets the transaction identifier associated with the event.
    /// </summary>
    [JsonPropertyName("transaction_id")]
    required public ProofTransactionId TransactionId { get; init; }

    /// <summary>
    /// Gets the timestamp when the event occurred.
    /// </summary>
    [JsonPropertyName("date_occurred")]
    required public string DateOccurred { get; init; }
}
