namespace Incursa.Integrations.ElectronicNotary.Tests;

using System.Text;
using FluentAssertions;
using Incursa.Integrations.ElectronicNotary.Proof.AspNetCore;
using Incursa.Integrations.ElectronicNotary.Proof.Contracts;
using Incursa.Integrations.ElectronicNotary.Proof.Types;
using Incursa.Platform.Webhooks;

[TestClass]
public sealed class ProofWebhookDispatchHandlerTests
{
    [TestMethod]
    public async Task KnownCompletedEventDispatchesTypedDtoAsync()
    {
        var recordingHandler = new RecordingProofWebhookHandler();
        var dispatchHandler = new ProofWebhookDispatchHandler(
            new[] { recordingHandler },
            new NoOpProofHealingTransactionRegistry());
        WebhookEventContext context = CreateContext("""{"event":"transaction.completed","data":{"transaction_id":"ot_123","date_occurred":"2026-02-06T00:00:00Z"}}""");

        await dispatchHandler.HandleAsync(context, CancellationToken.None).ConfigureAwait(false);

        recordingHandler.CompletedEvents.Should().ContainSingle();
        recordingHandler.CompletedEvents[0].TransactionId.Should().Be(new ProofTransactionId("ot_123"));
        recordingHandler.CompletedEvents[0].DateOccurred.Should().Be("2026-02-06T00:00:00Z");
        recordingHandler.UnknownEvents.Should().BeEmpty();
    }

    [TestMethod]
    public async Task UnknownEventDispatchesUnknownEnvelopeAsync()
    {
        var recordingHandler = new RecordingProofWebhookHandler();
        var dispatchHandler = new ProofWebhookDispatchHandler(
            new[] { recordingHandler },
            new NoOpProofHealingTransactionRegistry());
        WebhookEventContext context = CreateContext("""{"event":"transaction.custom_state_changed","data":{"transaction_id":"ot_123"}}""");

        await dispatchHandler.HandleAsync(context, CancellationToken.None).ConfigureAwait(false);

        recordingHandler.UnknownEvents.Should().ContainSingle();
        recordingHandler.UnknownEvents[0].Event.Should().Be("transaction.custom_state_changed");
    }

    private static WebhookEventContext CreateContext(string body)
    {
        return new WebhookEventContext(
            "proof",
            "dedupe-key",
            null,
            null,
            null,
            DateTimeOffset.UtcNow,
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
            Encoding.UTF8.GetBytes(body),
            "application/json");
    }

    private sealed class RecordingProofWebhookHandler : IProofWebhookHandler
    {
        public List<TransactionCompletedEvent> CompletedEvents { get; } = new List<TransactionCompletedEvent>();

        public List<TransactionReleasedEvent> ReleasedEvents { get; } = new List<TransactionReleasedEvent>();

        public List<TransactionCompletedWithRejectionsEvent> CompletedWithRejectionsEvents { get; } = new List<TransactionCompletedWithRejectionsEvent>();

        public List<ProofWebhookEnvelope> UnknownEvents { get; } = new List<ProofWebhookEnvelope>();

        public Task OnTransactionCompletedAsync(TransactionCompletedEvent evt)
        {
            this.CompletedEvents.Add(evt);
            return Task.CompletedTask;
        }

        public Task OnTransactionReleasedAsync(TransactionReleasedEvent evt)
        {
            this.ReleasedEvents.Add(evt);
            return Task.CompletedTask;
        }

        public Task OnTransactionCompletedWithRejectionsAsync(TransactionCompletedWithRejectionsEvent evt)
        {
            this.CompletedWithRejectionsEvents.Add(evt);
            return Task.CompletedTask;
        }

        public Task OnUnknownAsync(ProofWebhookEnvelope envelope)
        {
            this.UnknownEvents.Add(envelope);
            return Task.CompletedTask;
        }
    }
}
