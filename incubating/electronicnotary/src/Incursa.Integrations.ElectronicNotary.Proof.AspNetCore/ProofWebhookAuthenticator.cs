namespace Incursa.Integrations.ElectronicNotary.Proof.AspNetCore;

using System.Security.Cryptography;
using System.Text;
using Bravellian.Platform.Webhooks;
using Incursa.Integrations.ElectronicNotary.Proof;
using Microsoft.Extensions.Options;

internal sealed class ProofWebhookAuthenticator : IWebhookAuthenticator
{
    private const string AuthorizationHeaderName = "Authorization";
    private readonly IProofWebhookSignatureVerifier signatureVerifier;
    private readonly IOptions<ProofWebhookOptions> webhookOptions;
    private readonly IOptions<ProofClientOptions>? proofClientOptions;

    public ProofWebhookAuthenticator(
        IProofWebhookSignatureVerifier signatureVerifier,
        IOptions<ProofWebhookOptions> webhookOptions,
        IOptions<ProofClientOptions>? proofClientOptions = null)
    {
        this.signatureVerifier = signatureVerifier;
        this.webhookOptions = webhookOptions;
        this.proofClientOptions = proofClientOptions;
    }

    public Task<AuthResult> AuthenticateAsync(WebhookEnvelope envelope, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        static bool FixedTimeEquals(string left, string right)
        {
            byte[] leftBytes = Encoding.UTF8.GetBytes(left);
            byte[] rightBytes = Encoding.UTF8.GetBytes(right);
            return CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
        }

        static bool TryAuthenticateBearer(IReadOnlyDictionary<string, string> headers, string? expectedToken)
        {
            if (string.IsNullOrWhiteSpace(expectedToken))
            {
                return false;
            }

            if (!headers.TryGetValue(AuthorizationHeaderName, out string? authorizationHeader) || string.IsNullOrWhiteSpace(authorizationHeader))
            {
                return false;
            }

            const string BearerPrefix = "Bearer ";
            if (!authorizationHeader.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string providedToken = authorizationHeader[BearerPrefix.Length..].Trim();
            return FixedTimeEquals(providedToken, expectedToken);
        }

        ProofWebhookOptions options = this.webhookOptions.Value;
        if (TryAuthenticateBearer(envelope.Headers, options.BearerToken))
        {
            return Task.FromResult(new AuthResult(true, null));
        }

        if (!options.RequireSignature)
        {
            return Task.FromResult(new AuthResult(true, null));
        }

        if (!envelope.Headers.TryGetValue(ProofWebhookOptions.SignatureHeaderName, out string? signatureHeader))
        {
            return Task.FromResult(new AuthResult(false, $"Missing {ProofWebhookOptions.SignatureHeaderName} header."));
        }

        string signingKey = this.ResolveSigningKey(options);
        if (string.IsNullOrWhiteSpace(signingKey))
        {
            return Task.FromResult(new AuthResult(false, "No signing key is configured for Proof webhook verification."));
        }

        bool isValid = this.signatureVerifier.TryVerify(envelope.BodyBytes, signatureHeader, signingKey);
        return Task.FromResult(
            isValid
                ? new AuthResult(true, null)
                : new AuthResult(false, "Invalid webhook signature."));
    }

    private string ResolveSigningKey(ProofWebhookOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.SigningKey))
        {
            return options.SigningKey;
        }

        return this.proofClientOptions?.Value.ApiKey ?? string.Empty;
    }
}
