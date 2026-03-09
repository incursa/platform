namespace Incursa.Integrations.ElectronicNotary.Proof.AspNetCore;

using Incursa.Integrations.ElectronicNotary.Proof.Contracts;

/// <summary>
/// Handles typed Proof webhook events after they are ingested and dispatched.
/// </summary>
public interface IProofWebhookHandler
{
    /// <summary>
    /// Handles the <c>transaction.completed</c> webhook event.
    /// </summary>
    /// <param name="evt">The typed webhook event payload.</param>
    /// <returns>A task representing asynchronous handling.</returns>
    Task OnTransactionCompletedAsync(TransactionCompletedEvent evt);

    /// <summary>
    /// Handles the <c>transaction.released</c> webhook event.
    /// </summary>
    /// <param name="evt">The typed webhook event payload.</param>
    /// <returns>A task representing asynchronous handling.</returns>
    Task OnTransactionReleasedAsync(TransactionReleasedEvent evt);

    /// <summary>
    /// Handles the <c>transaction.completed_with_rejections</c> webhook event.
    /// </summary>
    /// <param name="evt">The typed webhook event payload.</param>
    /// <returns>A task representing asynchronous handling.</returns>
    Task OnTransactionCompletedWithRejectionsAsync(TransactionCompletedWithRejectionsEvent evt);

    /// <summary>
    /// Handles webhook events that do not match a typed event contract.
    /// </summary>
    /// <param name="envelope">The flexible webhook envelope.</param>
    /// <returns>A task representing asynchronous handling.</returns>
    Task OnUnknownAsync(ProofWebhookEnvelope envelope);
}
