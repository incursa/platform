namespace Incursa.Integrations.ElectronicNotary.Proof;

using System.Security.Cryptography;
using System.Text;

/// <summary>
/// Default Proof webhook signature verifier implementation.
/// </summary>
public sealed class ProofWebhookSignatureVerifier : IProofWebhookSignatureVerifier
{
    /// <inheritdoc />
    public bool TryVerify(ReadOnlyMemory<byte> body, string? signatureHeader, string signingKey)
    {
        if (string.IsNullOrWhiteSpace(signatureHeader) || string.IsNullOrEmpty(signingKey))
        {
            return false;
        }

        ReadOnlySpan<char> signatureHex = signatureHeader.AsSpan().Trim();
        if ((signatureHex.Length & 1) != 0)
        {
            return false;
        }

        byte[] providedSignature = new byte[signatureHex.Length / 2];
        if (!TryDecodeHex(signatureHex, providedSignature))
        {
            return false;
        }

        byte[] signingKeyBytes = Encoding.UTF8.GetBytes(signingKey);
        using var hmac = new HMACSHA256(signingKeyBytes);
        byte[] computedSignature = hmac.ComputeHash(body.ToArray());

        return CryptographicOperations.FixedTimeEquals(computedSignature, providedSignature);
    }

    private static bool TryDecodeHex(ReadOnlySpan<char> hex, Span<byte> destination)
    {
        if (hex.Length != destination.Length * 2)
        {
            return false;
        }

        for (int index = 0; index < destination.Length; index++)
        {
            int highNibble = HexToInt(hex[index * 2]);
            int lowNibble = HexToInt(hex[(index * 2) + 1]);
            if (highNibble < 0 || lowNibble < 0)
            {
                return false;
            }

            destination[index] = (byte)((highNibble << 4) | lowNibble);
        }

        return true;
    }

    private static int HexToInt(char value)
    {
        if (value is >= '0' and <= '9')
        {
            return value - '0';
        }

        if (value is >= 'a' and <= 'f')
        {
            return (value - 'a') + 10;
        }

        if (value is >= 'A' and <= 'F')
        {
            return (value - 'A') + 10;
        }

        return -1;
    }
}
