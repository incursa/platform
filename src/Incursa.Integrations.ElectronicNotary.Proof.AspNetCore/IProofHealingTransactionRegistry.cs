namespace Incursa.Integrations.ElectronicNotary.Proof.AspNetCore;

using Incursa.Integrations.ElectronicNotary.Proof.Types;

/// <summary>
/// Tracks Proof transactions for healing polling and terminal completion.
/// </summary>
public interface IProofHealingTransactionRegistry
{
    /// <summary>
    /// Registers a transaction so it can be picked up by the healing poller.
    /// </summary>
    /// <param name="transactionId">The transaction identifier to track.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A task representing asynchronous persistence.</returns>
    Task RegisterTransactionAsync(ProofTransactionId transactionId, CancellationToken cancellationToken);

    /// <summary>
    /// Marks a transaction as terminal because a terminal webhook event was received.
    /// </summary>
    /// <param name="transactionId">The transaction identifier to complete.</param>
    /// <param name="reason">A reason string, typically the webhook event name.</param>
    /// <param name="observedAtUtc">The UTC timestamp when the terminal event was observed.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A task representing asynchronous persistence.</returns>
    Task MarkTransactionTerminalAsync(
        ProofTransactionId transactionId,
        string reason,
        DateTimeOffset observedAtUtc,
        CancellationToken cancellationToken);

    /// <summary>
    /// Records a polling failure so the next poll can be delayed with backoff.
    /// </summary>
    /// <param name="transactionId">The transaction identifier that failed to poll.</param>
    /// <param name="errorMessage">The failure message to persist for diagnostics.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A task representing asynchronous persistence.</returns>
    Task RecordPollingFailureAsync(ProofTransactionId transactionId, string errorMessage, CancellationToken cancellationToken);
}
