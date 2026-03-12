namespace Incursa.Integrations.Stripe;

public sealed class StripeBillingOptions
{
    public string ApiKey { get; set; } = string.Empty;

    public string? WebhookSecret { get; set; }

    public string ApiBase { get; set; } = "https://api.stripe.com";
}
