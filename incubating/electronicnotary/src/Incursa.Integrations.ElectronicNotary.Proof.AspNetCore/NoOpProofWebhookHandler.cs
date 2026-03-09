namespace Incursa.Integrations.ElectronicNotary.Proof.AspNetCore;

using Incursa.Integrations.ElectronicNotary.Proof.Contracts;

internal sealed class NoOpProofWebhookHandler : IProofWebhookHandler
{
    public Task OnTransactionCompletedAsync(TransactionCompletedEvent evt)
    {
        return Task.CompletedTask;
    }

    public Task OnTransactionReleasedAsync(TransactionReleasedEvent evt)
    {
        return Task.CompletedTask;
    }

    public Task OnTransactionCompletedWithRejectionsAsync(TransactionCompletedWithRejectionsEvent evt)
    {
        return Task.CompletedTask;
    }

    public Task OnUnknownAsync(ProofWebhookEnvelope envelope)
    {
        return Task.CompletedTask;
    }
}
