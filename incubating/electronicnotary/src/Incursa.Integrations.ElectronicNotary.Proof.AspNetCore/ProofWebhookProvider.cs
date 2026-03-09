namespace Incursa.Integrations.ElectronicNotary.Proof.AspNetCore;

using Bravellian.Platform.Webhooks;

internal sealed class ProofWebhookProvider : WebhookProviderBase
{
    public ProofWebhookProvider(
        ProofWebhookAuthenticator authenticator,
        ProofWebhookClassifier classifier,
        ProofWebhookDispatchHandler dispatchHandler)
        : base(authenticator, classifier, new IWebhookHandler[] { dispatchHandler })
    {
    }

    public override string Name => ProofWebhookOptions.ProviderName;
}
