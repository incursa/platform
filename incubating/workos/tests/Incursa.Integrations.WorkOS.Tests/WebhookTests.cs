namespace Incursa.Integrations.WorkOS.Tests;

using System.Security.Cryptography;
using Incursa.Integrations.WorkOS.Core.Webhooks;

[TestClass]
public sealed class WebhookTests
{
    [TestMethod]
    public void Verify_ValidSignature_ReturnsTrue()
    {
        var secret = "whsec_test";
        var verifier = new WorkOsWebhookVerifier(secret);

        var body = Encoding.UTF8.GetBytes("{\"id\":\"evt_1\"}");
        var timestamp = "1700000000";
        var payload = Encoding.UTF8.GetBytes(timestamp + "." + Encoding.UTF8.GetString(body));
        var hash = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), payload);
        var signature = Convert.ToHexString(hash);

        var result = verifier.Verify(
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["WorkOS-Signature"] = $"t={timestamp},v1={signature}",
            },
            body);

        Assert.IsTrue(result.IsValid);
    }
}

