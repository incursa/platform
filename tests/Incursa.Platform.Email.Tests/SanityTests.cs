namespace Incursa.Platform.Email.Tests;

public sealed class SanityTests
{
    [Fact]
    public void PublicTypesAreLoadable()
    {
        var outboxOptions = new EmailOutboxOptions();
        var postmarkOptions = new global::Incursa.Platform.Email.Postmark.PostmarkOptions();
        var webhookOptions = new global::Incursa.Platform.Email.Postmark.PostmarkWebhookOptions();

        outboxOptions.ShouldNotBeNull();
        postmarkOptions.ShouldNotBeNull();
        webhookOptions.ShouldNotBeNull();
    }
}

