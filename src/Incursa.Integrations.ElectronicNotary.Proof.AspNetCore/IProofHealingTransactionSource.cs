namespace Incursa.Integrations.ElectronicNotary.Proof.AspNetCore;

using Incursa.Integrations.ElectronicNotary.Proof.Types;

/// <summary>
/// Supplies transaction identifiers that should be polled by the healing service.
/// </summary>
public interface IProofHealingTransactionSource
{
    /// <summary>
    /// Gets a batch of transaction identifiers that should be polled in the current cycle.
    /// </summary>
    /// <param name="maxCount">Maximum number of identifiers requested.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A collection of transaction identifiers to poll.</returns>
    Task<IReadOnlyCollection<ProofTransactionId>> GetTransactionIdsToPollAsync(int maxCount, CancellationToken cancellationToken);
}
