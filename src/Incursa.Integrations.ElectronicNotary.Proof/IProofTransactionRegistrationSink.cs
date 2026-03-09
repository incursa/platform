namespace Incursa.Integrations.ElectronicNotary.Proof;

using Incursa.Integrations.ElectronicNotary.Proof.Types;

/// <summary>
/// Registers newly created Proof transactions with downstream tracking stores.
/// </summary>
public interface IProofTransactionRegistrationSink
{
    /// <summary>
    /// Registers a transaction identifier for downstream processing.
    /// </summary>
    /// <param name="transactionId">The created Proof transaction identifier.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A task representing the asynchronous registration operation.</returns>
    Task RegisterTransactionAsync(ProofTransactionId transactionId, CancellationToken cancellationToken);
}
