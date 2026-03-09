namespace Incursa.Integrations.ElectronicNotary.Tests;

using System.Text;
using Bravellian.Platform.Webhooks;
using FluentAssertions;
using Incursa.Integrations.ElectronicNotary.Proof.AspNetCore;

[TestClass]
public sealed class ProofWebhookClassifierTests
{
    [TestMethod]
    public async Task ClassifierUsesStableKeyFromEventTransactionAndDateAsync()
    {
        var classifier = new ProofWebhookClassifier();
        WebhookEnvelope envelope = CreateEnvelope("""{"event":"transaction.updated","transaction_id":"ot_123","date_occurred":"2026-02-06T00:00:00Z"}""");

        ClassifyResult result = await classifier.ClassifyAsync(envelope, CancellationToken.None).ConfigureAwait(false);

        result.Decision.Should().Be(WebhookIngestDecision.Accepted);
        result.EventType.Should().Be("transaction.updated");
        result.DedupeKey.Should().Be("proof:transaction.updated:ot_123:2026-02-06T00:00:00Z");
    }

    [TestMethod]
    public async Task ClassifierFallsBackToBodyHashWhenTransactionOrDateMissingAsync()
    {
        var classifier = new ProofWebhookClassifier();
        WebhookEnvelope envelope = CreateEnvelope("""{"event":"transaction.updated"}""");

        ClassifyResult result = await classifier.ClassifyAsync(envelope, CancellationToken.None).ConfigureAwait(false);

        result.Decision.Should().Be(WebhookIngestDecision.Accepted);
        result.DedupeKey.Should().StartWith("proof:sha256:");
    }

    [TestMethod]
    public async Task ClassifierUsesNestedDataTransactionAndDateWhenAvailableAsync()
    {
        var classifier = new ProofWebhookClassifier();
        WebhookEnvelope envelope = CreateEnvelope("""{"event":"transaction.updated","data":{"transaction_id":"ot_nested123","date_occurred":"2026-02-06T01:02:03Z"}}""");

        ClassifyResult result = await classifier.ClassifyAsync(envelope, CancellationToken.None).ConfigureAwait(false);

        result.Decision.Should().Be(WebhookIngestDecision.Accepted);
        result.DedupeKey.Should().Be("proof:transaction.updated:ot_nested123:2026-02-06T01:02:03Z");
    }

    private static WebhookEnvelope CreateEnvelope(string body)
    {
        return new WebhookEnvelope(
            ProofWebhookOptions.ProviderName,
            DateTimeOffset.UtcNow,
            "POST",
            "/webhooks/proof",
            string.Empty,
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
            "application/json",
            Encoding.UTF8.GetBytes(body),
            "127.0.0.1");
    }
}
