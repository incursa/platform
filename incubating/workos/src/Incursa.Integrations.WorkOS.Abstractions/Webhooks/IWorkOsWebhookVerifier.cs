namespace Incursa.Integrations.WorkOS.Abstractions.Webhooks;

public interface IWorkOsWebhookVerifier
{
    WorkOsWebhookVerificationResult Verify(IReadOnlyDictionary<string, string> headers, ReadOnlyMemory<byte> body);
}

