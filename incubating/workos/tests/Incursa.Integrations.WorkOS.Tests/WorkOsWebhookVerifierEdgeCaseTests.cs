namespace Incursa.Integrations.WorkOS.Tests;

using System.Security.Cryptography;
using Incursa.Integrations.WorkOS.Core.Webhooks;

[TestClass]
public sealed class WorkOsWebhookVerifierEdgeCaseTests
{
    [TestMethod]
    public void Verify_MissingSignatureHeader_ReturnsMissingSignature()
    {
        var sut = new WorkOsWebhookVerifier("whsec_test");

        var result = sut.Verify(new Dictionary<string, string>(), Encoding.UTF8.GetBytes("{}"));

        Assert.IsFalse(result.IsValid);
        Assert.AreEqual("missing_signature", result.FailureReason);
    }

    [TestMethod]
    public void Verify_InvalidSignatureHeaderFormat_ReturnsInvalidSignatureFormat()
    {
        var sut = new WorkOsWebhookVerifier("whsec_test");

        var result = sut.Verify(
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["WorkOS-Signature"] = "v1=nothex",
            },
            Encoding.UTF8.GetBytes("{}"));

        Assert.IsFalse(result.IsValid);
        Assert.AreEqual("invalid_signature_format", result.FailureReason);
    }

    [TestMethod]
    public void Verify_MismatchedSignature_ReturnsSignatureMismatch()
    {
        var sut = new WorkOsWebhookVerifier("whsec_test");
        var body = Encoding.UTF8.GetBytes("{\"id\":\"evt_1\"}");
        var timestamp = "1700000000";
        var payload = Encoding.UTF8.GetBytes(timestamp + "." + Encoding.UTF8.GetString(body));
        var hash = HMACSHA256.HashData(Encoding.UTF8.GetBytes("different_secret"), payload);

        var result = sut.Verify(
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["WorkOS-Signature"] = $"t={timestamp},v1={Convert.ToHexString(hash)}",
            },
            body);

        Assert.IsFalse(result.IsValid);
        Assert.AreEqual("signature_mismatch", result.FailureReason);
    }
}
