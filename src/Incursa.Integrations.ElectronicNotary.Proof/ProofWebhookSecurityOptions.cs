namespace Incursa.Integrations.ElectronicNotary.Proof;

internal sealed class ProofWebhookSecurityOptions
{
    public string SigningKey { get; set; } = string.Empty;

    public bool RequireSignature { get; set; } = true;
}
