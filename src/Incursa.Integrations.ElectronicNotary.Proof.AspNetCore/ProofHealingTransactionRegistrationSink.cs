namespace Incursa.Integrations.ElectronicNotary.Proof.AspNetCore;

using Incursa.Integrations.ElectronicNotary.Proof;
using Incursa.Integrations.ElectronicNotary.Proof.Types;

internal sealed class ProofHealingTransactionRegistrationSink : IProofTransactionRegistrationSink
{
    private readonly IProofHealingTransactionRegistry registry;

    public ProofHealingTransactionRegistrationSink(IProofHealingTransactionRegistry registry)
    {
        this.registry = registry;
    }

    public Task RegisterTransactionAsync(ProofTransactionId transactionId, CancellationToken cancellationToken)
    {
        return this.registry.RegisterTransactionAsync(transactionId, cancellationToken);
    }
}
