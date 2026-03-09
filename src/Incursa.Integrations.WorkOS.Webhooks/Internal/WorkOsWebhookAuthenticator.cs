namespace Incursa.Integrations.WorkOS.Webhooks.Internal;

using System.Security.Cryptography;
using System.Text;
using Incursa.Platform.Webhooks;
using Microsoft.Extensions.Options;

internal sealed class WorkOsWebhookAuthenticator : IWebhookAuthenticator
{
    private readonly IOptions<WorkOsWebhookOptions> options;

    public WorkOsWebhookAuthenticator(IOptions<WorkOsWebhookOptions> options)
    {
        this.options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public Task<AuthResult> AuthenticateAsync(WebhookEnvelope envelope, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        cancellationToken.ThrowIfCancellationRequested();

        var signingSecret = options.Value.SigningSecret;
        if (string.IsNullOrWhiteSpace(signingSecret))
        {
            return Task.FromResult(new AuthResult(false, "missing_signing_secret"));
        }

        if (!TryGetSignatureHeader(envelope.Headers, out var rawSignature))
        {
            return Task.FromResult(new AuthResult(false, "missing_signature"));
        }

        if (!TryParseSignature(rawSignature, out var timestamp, out var expectedSignatureHex))
        {
            return Task.FromResult(new AuthResult(false, "invalid_signature_format"));
        }

        var payload = Encoding.UTF8.GetBytes($"{timestamp}.{Encoding.UTF8.GetString(envelope.BodyBytes)}");
        var computedSignature = HMACSHA256.HashData(Encoding.UTF8.GetBytes(signingSecret.Trim()), payload);
        var expectedSignature = Convert.FromHexString(expectedSignatureHex);

        var isAuthenticated = CryptographicOperations.FixedTimeEquals(computedSignature, expectedSignature);
        return Task.FromResult(
            isAuthenticated
                ? new AuthResult(true, null)
                : new AuthResult(false, "signature_mismatch"));
    }

    private static bool TryGetSignatureHeader(IReadOnlyDictionary<string, string> headers, out string signature)
    {
        ArgumentNullException.ThrowIfNull(headers);

        if (headers.TryGetValue(WorkOsWebhookDefaults.SignatureHeaderName, out signature!))
        {
            return !string.IsNullOrWhiteSpace(signature);
        }

        if (headers.TryGetValue(WorkOsWebhookDefaults.SignatureHeaderName.ToLowerInvariant(), out signature!))
        {
            return !string.IsNullOrWhiteSpace(signature);
        }

        signature = string.Empty;
        return false;
    }

    private static bool TryParseSignature(string rawSignature, out string timestamp, out string signatureHex)
    {
        timestamp = string.Empty;
        signatureHex = string.Empty;

        foreach (var part in rawSignature.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
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
