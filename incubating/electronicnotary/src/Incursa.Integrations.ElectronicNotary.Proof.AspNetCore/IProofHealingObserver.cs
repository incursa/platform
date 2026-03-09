namespace Incursa.Integrations.ElectronicNotary.Proof.AspNetCore;

using Incursa.Integrations.ElectronicNotary.Proof.Contracts;
using Incursa.Integrations.ElectronicNotary.Proof.Types;

/// <summary>
/// Observes transaction snapshots loaded by the healing poller.
/// </summary>
public interface IProofHealingObserver
{
    /// <summary>
    /// Handles a transaction loaded during a healing polling cycle.
    /// </summary>
    /// <param name="transactionId">The transaction identifier that was polled.</param>
    /// <param name="transaction">The transaction response returned by Proof.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A task representing asynchronous handling.</returns>
    Task OnTransactionPolledAsync(ProofTransactionId transactionId, Transaction transaction, CancellationToken cancellationToken);
}
