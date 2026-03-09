namespace Incursa.Platform.Webhooks.WorkOS.Internal;

internal sealed class WorkOsWebhookProvider : WebhookProviderBase
{
    public WorkOsWebhookProvider(
        WorkOsWebhookAuthenticator authenticator,
        WorkOsWebhookClassifier classifier,
        IEnumerable<IWorkOsWebhookHandler> handlers)
        : base(authenticator, classifier, handlers.Cast<IWebhookHandler>().ToArray())
    {
    }

    public override string Name => WorkOsWebhookDefaults.ProviderName;
}
