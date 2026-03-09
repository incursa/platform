namespace Incursa.Integrations.ElectronicNotary.Proof;

using Incursa.Integrations.ElectronicNotary.Proof.Types;

internal sealed class NoOpProofTransactionRegistrationSink : IProofTransactionRegistrationSink
{
    public Task RegisterTransactionAsync(ProofTransactionId transactionId, CancellationToken cancellationToken)
    {
        _ = transactionId;
        _ = cancellationToken;
        return Task.CompletedTask;
    }
}
