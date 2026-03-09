namespace Incursa.Integrations.ElectronicNotary.Proof.AspNetCore;

using Incursa.Integrations.ElectronicNotary.Proof.Types;

internal sealed class NoOpProofHealingTransactionRegistry : IProofHealingTransactionRegistry
{
    public Task RegisterTransactionAsync(ProofTransactionId transactionId, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task MarkTransactionTerminalAsync(
        ProofTransactionId transactionId,
        string reason,
        DateTimeOffset observedAtUtc,
        CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task RecordPollingFailureAsync(ProofTransactionId transactionId, string errorMessage, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
