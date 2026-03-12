namespace Incursa.Integrations.Stripe;

public sealed record StripeCheckoutSessionRequest(
    string SuccessUrl,
    string CancelUrl,
    IReadOnlyList<StripeCheckoutLineItem> LineItems,
    string? CustomerId = null,
    string? CustomerEmail = null,
    string? ClientReferenceId = null,
    IReadOnlyDictionary<string, string>? Metadata = null,
    bool AllowPromotionCodes = true);

public sealed record StripeCheckoutLineItem(string PriceId, long Quantity);

public sealed record StripeCheckoutSessionResult(
    string SessionId,
    string? Url,
    string? CustomerId,
    string? SubscriptionId,
    string? ClientReferenceId,
    IReadOnlyDictionary<string, string> Metadata);

public sealed record StripePortalSessionRequest(string CustomerId, string ReturnUrl);

public sealed record StripePortalSessionResult(string Url);

public sealed record StripeWebhookEnvelope(
    string EventId,
    string EventType,
    string? CustomerId,
    string? SubscriptionId,
    string? CheckoutSessionId,
    string? ClientReferenceId,
    IReadOnlyDictionary<string, string> Metadata);

public sealed record StripeSubscriptionSnapshot(
    string CustomerId,
    string SubscriptionId,
    string Status,
    DateTimeOffset? CurrentPeriodStartUtc,
    DateTimeOffset? CurrentPeriodEndUtc,
    bool CancelAtPeriodEnd,
    IReadOnlyList<StripeSubscriptionItemSnapshot> Items,
    DateTimeOffset SyncedAtUtc);

public sealed record StripeSubscriptionItemSnapshot(
    string SubscriptionItemId,
    string? PriceId,
    string? ProductId,
    long Quantity);
