namespace Incursa.Integrations.Stripe;

public interface IStripeBillingClient
{
    StripeWebhookEnvelope ParseWebhook(string payloadJson, string stripeSignatureHeader);

    Task<StripeSubscriptionSnapshot?> GetSubscriptionAsync(string subscriptionId, CancellationToken cancellationToken = default);

    Task<StripeCheckoutSessionResult> CreateCheckoutSessionAsync(
        StripeCheckoutSessionRequest request,
        CancellationToken cancellationToken = default);

    Task<StripePortalSessionResult> CreateBillingPortalSessionAsync(
        StripePortalSessionRequest request,
        CancellationToken cancellationToken = default);
}
