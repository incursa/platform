namespace Incursa.Integrations.ElectronicNotary.Tests;

using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using Incursa.Integrations.ElectronicNotary.Proof;

[TestClass]
public sealed class ProofWebhookSignatureVerifierTests
{
    [TestMethod]
    public void TryVerifyReturnsTrueForValidSignature()
    {
        var verifier = new ProofWebhookSignatureVerifier();
        byte[] body = Encoding.UTF8.GetBytes("""{"event":"transaction.complete"}""");
        string signingKey = "proof-signing-key";
        string signature = CreateHexSignature(body, signingKey).ToUpperInvariant();

        bool isValid = verifier.TryVerify(body, signature, signingKey);

        isValid.Should().BeTrue();
    }

    [TestMethod]
    public void TryVerifyReturnsFalseForInvalidSignature()
    {
        var verifier = new ProofWebhookSignatureVerifier();
        byte[] body = Encoding.UTF8.GetBytes("""{"event":"transaction.complete"}""");

        bool isValid = verifier.TryVerify(body, "00", "proof-signing-key");

        isValid.Should().BeFalse();
    }

    [TestMethod]
    public void MissingSignatureFailsWhenRequireSignatureIsTrue()
    {
        var verifier = new ProofWebhookSignatureVerifier();
        byte[] body = Encoding.UTF8.GetBytes("""{"event":"transaction.complete"}""");
        var options = new ProofWebhookSecurityOptions
        {
            SigningKey = "proof-signing-key",
            RequireSignature = true,
        };

        bool isValid = !options.RequireSignature || verifier.TryVerify(body, null, options.SigningKey);

        isValid.Should().BeFalse();
    }

    private static string CreateHexSignature(ReadOnlySpan<byte> body, string signingKey)
    {
        byte[] signingKeyBytes = Encoding.UTF8.GetBytes(signingKey);
        using var hmac = new HMACSHA256(signingKeyBytes);
        byte[] hash = hmac.ComputeHash(body.ToArray());
        return Convert.ToHexString(hash);
    }
}
