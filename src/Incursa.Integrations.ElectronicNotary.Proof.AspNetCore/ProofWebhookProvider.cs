namespace Incursa.Integrations.ElectronicNotary.Proof.AspNetCore;

using Incursa.Platform.Webhooks;

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
