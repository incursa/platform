namespace Incursa.Integrations.WorkOS.Tests;

using System.Security.Cryptography;
using Incursa.Integrations.WorkOS.Abstractions.Configuration;
using Incursa.Integrations.WorkOS.Core.Authorization;
using Incursa.Integrations.WorkOS.Core.Webhooks;

[TestClass]
public sealed class FuzzBehaviorTests
{
    [TestMethod]
    [TestCategory("Fuzz")]
    public void WorkOsWebhookVerifier_RandomizedHeadersAndBodies_DoesNotThrow()
    {
        var rng = new Random(12345);
        var sut = new WorkOsWebhookVerifier("whsec_test");

        for (var i = 0; i < 2000; i++)
        {
            var bodyBytes = new byte[rng.Next(0, 512)];
            rng.NextBytes(bodyBytes);
            var body = Convert.ToBase64String(bodyBytes);
            var header = RandomSignatureHeader(rng, body);

            _ = sut.Verify(
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["WorkOS-Signature"] = header,
                },
                Encoding.UTF8.GetBytes(body));
        }
    }

    [TestMethod]
    [TestCategory("Fuzz")]
    public void WorkOsPermissionMapper_RandomizedPermissions_DoesNotThrowAndReturnsStableOrdering()
    {
        var rng = new Random(54321);
        var sut = new WorkOsPermissionMapper(WorkOsPermissionMappingOptions.CreateDefaultArtifacts());

        for (var i = 0; i < 500; i++)
        {
            var input = Enumerable.Range(0, 50)
                .Select(_ => RandomToken(rng))
                .ToArray();

            var scopes = sut.MapToScopes(input, strictMode: false, out _).ToArray();
            var sorted = scopes.OrderBy(static x => x, StringComparer.Ordinal).ToArray();

            CollectionAssert.AreEqual(sorted, scopes);
        }
    }

    private static string RandomSignatureHeader(Random rng, string body)
    {
        var timestamp = rng.Next(1600000000, 1800000000).ToString(CultureInfo.InvariantCulture);
        if (rng.NextDouble() < 0.5)
        {
            return $"t={timestamp},v1={RandomToken(rng)}";
        }

        var payload = Encoding.UTF8.GetBytes($"{timestamp}.{body}");
        var hash = HMACSHA256.HashData(Encoding.UTF8.GetBytes("whsec_test"), payload);
        return $"t={timestamp},v1={Convert.ToHexString(hash)}";
    }

    private static string RandomToken(Random rng)
    {
        const string chars = "abcdefghijklmnopqrstuvwxyz0123456789:-_[]{}@!";
        var length = rng.Next(0, 20);
        Span<char> buffer = stackalloc char[length];
        for (var i = 0; i < length; i++)
        {
            buffer[i] = chars[rng.Next(chars.Length)];
        }

        return new string(buffer);
    }
}
