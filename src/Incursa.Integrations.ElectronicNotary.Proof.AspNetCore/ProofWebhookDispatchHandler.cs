namespace Incursa.Integrations.ElectronicNotary.Proof.AspNetCore;

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Incursa.Platform.Webhooks;
using Incursa.Integrations.ElectronicNotary.Proof.Contracts;

internal sealed class ProofWebhookDispatchHandler : IWebhookHandler
{
    private const string TransactionCompleted = "transaction.completed";
    private const string TransactionReleased = "transaction.released";
    private const string TransactionCompletedWithRejections = "transaction.completed_with_rejections";
    private static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
    private readonly IReadOnlyList<IProofWebhookHandler> handlers;
    private readonly IProofHealingTransactionRegistry transactionRegistry;

    public ProofWebhookDispatchHandler(
        IEnumerable<IProofWebhookHandler> handlers,
        IProofHealingTransactionRegistry transactionRegistry)
    {
        this.handlers = handlers.ToArray();
        this.transactionRegistry = transactionRegistry;
    }

    public bool CanHandle(string eventType)
    {
        return true;
    }

    public async Task HandleAsync(WebhookEventContext context, CancellationToken cancellationToken)
    {
        ProofWebhookEnvelope envelope = ParseEnvelope(context.BodyBytes);

        foreach (IProofWebhookHandler handler in this.handlers)
        {
            switch (envelope.Event)
            {
                case TransactionCompleted:
                    {
                        if (TryDeserialize(envelope.Data, out TransactionCompletedEvent? completedEvent))
                        {
                            await this.transactionRegistry
                                .MarkTransactionTerminalAsync(
                                    completedEvent.TransactionId,
                                    TransactionCompleted,
                                    DateTimeOffset.UtcNow,
                                    cancellationToken)
                                .ConfigureAwait(false);
                            await handler.OnTransactionCompletedAsync(completedEvent).ConfigureAwait(false);
                        }
                        else
                        {
                            await handler.OnUnknownAsync(envelope).ConfigureAwait(false);
                        }

                        break;
                    }

                case TransactionReleased:
                    {
                        if (TryDeserialize(envelope.Data, out TransactionReleasedEvent? releasedEvent))
                        {
                            await this.transactionRegistry
                                .MarkTransactionTerminalAsync(
                                    releasedEvent.TransactionId,
                                    TransactionReleased,
                                    DateTimeOffset.UtcNow,
                                    cancellationToken)
                                .ConfigureAwait(false);
                            await handler.OnTransactionReleasedAsync(releasedEvent).ConfigureAwait(false);
                        }
                        else
                        {
                            await handler.OnUnknownAsync(envelope).ConfigureAwait(false);
                        }

                        break;
                    }

                case TransactionCompletedWithRejections:
                    {
                        if (TryDeserialize(envelope.Data, out TransactionCompletedWithRejectionsEvent? rejectedEvent))
                        {
                            await this.transactionRegistry
                                .MarkTransactionTerminalAsync(
                                    rejectedEvent.TransactionId,
                                    TransactionCompletedWithRejections,
                                    DateTimeOffset.UtcNow,
                                    cancellationToken)
                                .ConfigureAwait(false);
                            await handler.OnTransactionCompletedWithRejectionsAsync(rejectedEvent).ConfigureAwait(false);
                        }
                        else
                        {
                            await handler.OnUnknownAsync(envelope).ConfigureAwait(false);
                        }

                        break;
                    }

                default:
                    await handler.OnUnknownAsync(envelope).ConfigureAwait(false);
                    break;
            }
        }
    }

    private static ProofWebhookEnvelope ParseEnvelope(byte[] bodyBytes)
    {
        using JsonDocument document = JsonDocument.Parse(bodyBytes);
        JsonElement root = document.RootElement;
        string eventName = root.TryGetProperty("event", out JsonElement eventProperty) && eventProperty.ValueKind == JsonValueKind.String
            ? eventProperty.GetString() ?? string.Empty
            : string.Empty;
        JsonElement data = root.TryGetProperty("data", out JsonElement dataProperty)
            ? dataProperty.Clone()
            : root.Clone();

        return new ProofWebhookEnvelope
        {
            Event = eventName,
            Data = data,
        };
    }

    private static bool TryDeserialize<TEvent>(JsonElement data, [NotNullWhen(true)] out TEvent? evt)
        where TEvent : class
    {
        evt = JsonSerializer.Deserialize<TEvent>(data.GetRawText(), SerializerOptions);
        if (evt is null)
        {
            return false;
        }

        return evt switch
        {
            TransactionCompletedEvent completed =>
                !string.IsNullOrWhiteSpace(completed.TransactionId.Value) &&
                !string.IsNullOrWhiteSpace(completed.DateOccurred),
            TransactionReleasedEvent released =>
                !string.IsNullOrWhiteSpace(released.TransactionId.Value) &&
                !string.IsNullOrWhiteSpace(released.DateOccurred),
            TransactionCompletedWithRejectionsEvent withRejections =>
                !string.IsNullOrWhiteSpace(withRejections.TransactionId.Value) &&
                !string.IsNullOrWhiteSpace(withRejections.DateOccurred),
            _ => true,
        };
    }
}
