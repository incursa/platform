namespace Incursa.Integrations.WorkOS.Core.Webhooks;

using System.Security.Cryptography;
using Incursa.Integrations.WorkOS.Abstractions.Webhooks;

public sealed class WorkOsWebhookVerifier : IWorkOsWebhookVerifier
{
    private readonly string _signingSecret;

    public WorkOsWebhookVerifier(string signingSecret)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(signingSecret);
        _signingSecret = signingSecret;
    }

    public WorkOsWebhookVerificationResult Verify(IReadOnlyDictionary<string, string> headers, ReadOnlyMemory<byte> body)
    {
        if (!headers.TryGetValue("workos-signature", out var signatureHeader) && !headers.TryGetValue("WorkOS-Signature", out signatureHeader))
        {
            return new WorkOsWebhookVerificationResult(false, "missing_signature");
        }

        if (!TryParseSignature(signatureHeader, out var timestamp, out var expectedSignatureHex))
        {
            return new WorkOsWebhookVerificationResult(false, "invalid_signature_format");
        }

        var payload = Encoding.UTF8.GetBytes($"{timestamp}.{Encoding.UTF8.GetString(body.Span)}");
        var secretBytes = Encoding.UTF8.GetBytes(_signingSecret);
        var computed = HMACSHA256.HashData(secretBytes, payload);
        var expected = Convert.FromHexString(expectedSignatureHex);
        var valid = CryptographicOperations.FixedTimeEquals(computed, expected);

        return valid
            ? new WorkOsWebhookVerificationResult(true, null)
            : new WorkOsWebhookVerificationResult(false, "signature_mismatch");
    }

    private static bool TryParseSignature(string raw, out string timestamp, out string signatureHex)
    {
        timestamp = string.Empty;
        signatureHex = string.Empty;

        foreach (var part in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var tokens = part.Split('=', 2, StringSplitOptions.TrimEntries);
            if (tokens.Length != 2)
            {
                continue;
            }

            if (string.Equals(tokens[0], "t", StringComparison.OrdinalIgnoreCase))
            {
                timestamp = tokens[1];
            }
            else if (string.Equals(tokens[0], "v1", StringComparison.OrdinalIgnoreCase))
            {
                signatureHex = tokens[1];
            }
        }

        return !string.IsNullOrWhiteSpace(timestamp)
            && !string.IsNullOrWhiteSpace(signatureHex)
            && signatureHex.Length % 2 == 0
            && signatureHex.All(static c => Uri.IsHexDigit(c));
    }
}

