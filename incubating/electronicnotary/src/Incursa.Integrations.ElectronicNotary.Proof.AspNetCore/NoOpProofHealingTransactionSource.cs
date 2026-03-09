namespace Incursa.Integrations.ElectronicNotary.Proof.AspNetCore;

using Incursa.Integrations.ElectronicNotary.Proof.Types;

internal sealed class NoOpProofHealingTransactionSource : IProofHealingTransactionSource
{
    private static readonly IReadOnlyCollection<ProofTransactionId> Empty = Array.Empty<ProofTransactionId>();

    public Task<IReadOnlyCollection<ProofTransactionId>> GetTransactionIdsToPollAsync(int maxCount, CancellationToken cancellationToken)
    {
        return Task.FromResult(Empty);
    }
}
