namespace Incursa.Integrations.ElectronicNotary.Proof.AspNetCore;

/// <summary>
/// Configures authentication behavior for incoming Proof webhooks.
/// </summary>
public sealed class ProofWebhookOptions
{
    /// <summary>
    /// The provider name used by the webhook ingestion pipeline.
    /// </summary>
    public const string ProviderName = "proof";

    /// <summary>
    /// The signature header expected on Proof webhook requests.
    /// </summary>
    public const string SignatureHeaderName = "X-Notarize-Signature";

    /// <summary>
    /// Gets or sets the signing key used to verify webhook signatures.
    /// When omitted, the Proof client API key is used when available.
    /// </summary>
    public string? SigningKey { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether webhook signatures are required.
    /// </summary>
    public bool RequireSignature { get; set; } = true;

    /// <summary>
    /// Gets or sets an optional bearer token accepted for webhook authentication.
    /// </summary>
    public string? BearerToken { get; set; }
}
