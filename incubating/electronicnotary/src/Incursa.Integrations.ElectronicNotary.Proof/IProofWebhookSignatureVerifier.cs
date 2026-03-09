namespace Incursa.Integrations.ElectronicNotary.Proof;

/// <summary>
/// Verifies Proof webhook signatures using HMAC-SHA256 over the raw request body.
/// </summary>
public interface IProofWebhookSignatureVerifier
{
    /// <summary>
    /// Attempts to verify a webhook signature header against the provided body and signing key.
    /// </summary>
    /// <param name="body">The raw request body bytes.</param>
    /// <param name="signatureHeader">The signature header value from the request.</param>
    /// <param name="signingKey">The Proof signing key.</param>
    /// <returns><see langword="true"/> when signature verification succeeds; otherwise <see langword="false"/>.</returns>
    bool TryVerify(ReadOnlyMemory<byte> body, string? signatureHeader, string signingKey);
}
