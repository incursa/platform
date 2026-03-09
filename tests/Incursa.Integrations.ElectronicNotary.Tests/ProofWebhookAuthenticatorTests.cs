namespace Incursa.Integrations.ElectronicNotary.Tests;

using Bravellian.Platform.Webhooks;
using FluentAssertions;
using Incursa.Integrations.ElectronicNotary.Proof;
using Incursa.Integrations.ElectronicNotary.Proof.AspNetCore;
using Microsoft.Extensions.Options;

[TestClass]
public sealed class ProofWebhookAuthenticatorTests
{
    [TestMethod]
    public async Task AuthenticateAcceptsBearerTokenWhenConfiguredAsync()
    {
        var verifier = new RecordingVerifier();
        var authenticator = new ProofWebhookAuthenticator(
            verifier,
            Options.Create(new ProofWebhookOptions
            {
                RequireSignature = true,
                BearerToken = "token-123",
            }),
            null);

        WebhookEnvelope envelope = CreateEnvelope(
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Authorization"] = "Bearer token-123",
            });

        AuthResult result = await authenticator.AuthenticateAsync(envelope, CancellationToken.None).ConfigureAwait(false);

        result.IsAuthenticated.Should().BeTrue();
        verifier.Calls.Should().Be(0);
    }

    [TestMethod]
    public async Task AuthenticateFallsBackToClientApiKeyForSignatureVerificationAsync()
    {
        var verifier = new RecordingVerifier { VerificationResult = true };
        var authenticator = new ProofWebhookAuthenticator(
            verifier,
            Options.Create(new ProofWebhookOptions
            {
                RequireSignature = true,
                SigningKey = null,
            }),
            Options.Create(new ProofClientOptions
            {
                ApiKey = "api-key-fallback",
            }));

        WebhookEnvelope envelope = CreateEnvelope(
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [ProofWebhookOptions.SignatureHeaderName] = "ABCDEF",
            });

        AuthResult result = await authenticator.AuthenticateAsync(envelope, CancellationToken.None).ConfigureAwait(false);

        result.IsAuthenticated.Should().BeTrue();
        verifier.Calls.Should().Be(1);
        verifier.LastSigningKey.Should().Be("api-key-fallback");
        verifier.LastSignatureHeader.Should().Be("ABCDEF");
    }

    private static WebhookEnvelope CreateEnvelope(IReadOnlyDictionary<string, string> headers)
    {
        return new WebhookEnvelope(
            ProofWebhookOptions.ProviderName,
            DateTimeOffset.UtcNow,
            "POST",
            "/webhooks/proof",
            string.Empty,
            headers,
            "application/json",
            """{"event":"transaction.updated"}"""u8.ToArray(),
            "127.0.0.1");
    }

    private sealed class RecordingVerifier : IProofWebhookSignatureVerifier
    {
        public bool VerificationResult { get; set; }

        public int Calls { get; private set; }

        public string? LastSignatureHeader { get; private set; }

        public string? LastSigningKey { get; private set; }

        public bool TryVerify(ReadOnlyMemory<byte> body, string? signatureHeader, string signingKey)
        {
            this.Calls++;
            this.LastSignatureHeader = signatureHeader;
            this.LastSigningKey = signingKey;
            return this.VerificationResult;
        }
    }
}
