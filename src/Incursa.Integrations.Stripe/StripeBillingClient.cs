using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;
using BillingPortalSessionService = Stripe.BillingPortal.SessionService;
using BillingPortalSessionCreateOptions = Stripe.BillingPortal.SessionCreateOptions;
using CheckoutSessionService = Stripe.Checkout.SessionService;
using CheckoutSessionCreateOptions = Stripe.Checkout.SessionCreateOptions;
using CheckoutSessionLineItemOptions = Stripe.Checkout.SessionLineItemOptions;
using StripeEvent = Stripe.Event;
using StripeEventUtility = Stripe.EventUtility;

namespace Incursa.Integrations.Stripe;

internal sealed class StripeBillingClient : IStripeBillingClient
{
    private readonly StripeBillingOptions options;
    private readonly StripeClient stripeClient;

    public StripeBillingClient(IOptions<StripeBillingOptions> options)
    {
        this.options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        if (string.IsNullOrWhiteSpace(this.options.ApiKey))
        {
            throw new InvalidOperationException("Stripe API key is required.");
        }

        stripeClient = string.IsNullOrWhiteSpace(this.options.ApiBase)
            ? new StripeClient(this.options.ApiKey)
            : new StripeClient(this.options.ApiKey, httpClient: null, apiBase: this.options.ApiBase);
    }

    public StripeWebhookEnvelope ParseWebhook(string payloadJson, string stripeSignatureHeader)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
        {
            throw new ArgumentException("Webhook payload is required.", nameof(payloadJson));
        }

        if (string.IsNullOrWhiteSpace(stripeSignatureHeader))
        {
            throw new ArgumentException("Stripe signature header is required.", nameof(stripeSignatureHeader));
        }

        if (string.IsNullOrWhiteSpace(options.WebhookSecret))
        {
            throw new InvalidOperationException("Stripe webhook secret is required.");
        }

        StripeEvent stripeEvent = StripeEventUtility.ConstructEvent(payloadJson, stripeSignatureHeader, options.WebhookSecret);
        Dictionary<string, string> metadata = [];
        string? customerId = null;
        string? subscriptionId = null;
        string? checkoutSessionId = null;
        string? clientReferenceId = null;

        switch (stripeEvent.Data.Object)
        {
            case Session session:
                customerId = session.CustomerId;
                subscriptionId = session.SubscriptionId;
                checkoutSessionId = session.Id;
                clientReferenceId = session.ClientReferenceId;
                AddMetadata(metadata, session.Metadata);
                break;
            case Subscription subscription:
                customerId = subscription.CustomerId;
                subscriptionId = subscription.Id;
                AddMetadata(metadata, subscription.Metadata);
                break;
            case Invoice invoice:
                customerId = invoice.CustomerId;
                subscriptionId = invoice.Parent?.SubscriptionDetails?.SubscriptionId;
                AddMetadata(metadata, invoice.Metadata);
                break;
        }

        return new StripeWebhookEnvelope(
            stripeEvent.Id,
            stripeEvent.Type,
            customerId,
            subscriptionId,
            checkoutSessionId,
            clientReferenceId,
            metadata);
    }

    public async Task<StripeSubscriptionSnapshot?> GetSubscriptionAsync(
        string subscriptionId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(subscriptionId))
        {
            throw new ArgumentException("Subscription id is required.", nameof(subscriptionId));
        }

        SubscriptionService service = new(stripeClient);
        Subscription? subscription = await service.GetAsync(
            subscriptionId.Trim(),
            new SubscriptionGetOptions
            {
                Expand = ["items.data.price.product"],
            },
            cancellationToken: cancellationToken).ConfigureAwait(false);
        if (subscription is null)
        {
            return null;
        }

        return ToSnapshot(subscription);
    }

    public async Task<StripeCheckoutSessionResult> CreateCheckoutSessionAsync(
        StripeCheckoutSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.LineItems.Count == 0)
        {
            throw new InvalidOperationException("At least one Stripe checkout line item is required.");
        }

        CheckoutSessionService service = new(stripeClient);
        Session session = await service.CreateAsync(
            new CheckoutSessionCreateOptions
            {
                Mode = "subscription",
                SuccessUrl = request.SuccessUrl,
                CancelUrl = request.CancelUrl,
                Customer = request.CustomerId,
                CustomerEmail = request.CustomerEmail,
                ClientReferenceId = request.ClientReferenceId,
                AllowPromotionCodes = request.AllowPromotionCodes,
                Metadata = request.Metadata is null ? null : new Dictionary<string, string>(request.Metadata, StringComparer.Ordinal),
                LineItems = request.LineItems
                    .Select(static item => new CheckoutSessionLineItemOptions
                    {
                        Price = item.PriceId,
                        Quantity = item.Quantity,
                    })
                    .ToList(),
            },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return new StripeCheckoutSessionResult(
            session.Id,
            session.Url,
            session.CustomerId,
            session.SubscriptionId,
            session.ClientReferenceId,
            session.Metadata is null
                ? new Dictionary<string, string>(StringComparer.Ordinal)
                : new Dictionary<string, string>(session.Metadata, StringComparer.Ordinal));
    }

    public async Task<StripePortalSessionResult> CreateBillingPortalSessionAsync(
        StripePortalSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.CustomerId))
        {
            throw new ArgumentException("Customer id is required.", nameof(request));
        }

        BillingPortalSessionService service = new(stripeClient);
        global::Stripe.BillingPortal.Session session = await service.CreateAsync(
            new BillingPortalSessionCreateOptions
            {
                Customer = request.CustomerId,
                ReturnUrl = request.ReturnUrl,
            },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return new StripePortalSessionResult(session.Url);
    }

    private static void AddMetadata(IDictionary<string, string> target, IDictionary<string, string>? source)
    {
        if (source is null)
        {
            return;
        }

        foreach ((string key, string value) in source)
        {
            target[key] = value;
        }
    }

    private static StripeSubscriptionSnapshot ToSnapshot(Subscription subscription)
    {
        IReadOnlyList<SubscriptionItem> items = subscription.Items?.Data ?? [];
        DateTimeOffset? currentPeriodStartUtc = items.Count > 0
            ? items.Select(static item => new DateTimeOffset(item.CurrentPeriodStart)).Min()
            : null;
        DateTimeOffset? currentPeriodEndUtc = items.Count > 0
            ? items.Select(static item => new DateTimeOffset(item.CurrentPeriodEnd)).Max()
            : null;

        return new StripeSubscriptionSnapshot(
            subscription.CustomerId ?? string.Empty,
            subscription.Id,
            subscription.Status ?? "unknown",
            currentPeriodStartUtc,
            currentPeriodEndUtc,
            subscription.CancelAtPeriodEnd,
            items
                .Select(static item => new StripeSubscriptionItemSnapshot(
                    item.Id,
                    item.Price?.Id,
                    item.Price?.ProductId,
                    item.Quantity))
                .ToArray(),
            DateTimeOffset.UtcNow);
    }
}
