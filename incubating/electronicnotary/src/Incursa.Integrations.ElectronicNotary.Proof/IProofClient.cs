namespace Incursa.Integrations.ElectronicNotary.Proof;

using Incursa.Integrations.ElectronicNotary.Proof.Contracts;
using Incursa.Integrations.ElectronicNotary.Proof.Types;

/// <summary>
/// Defines operations for interacting with the Proof transactions API.
/// </summary>
public interface IProofClient
{
    /// <summary>
    /// Creates a Proof transaction from the provided request payload.
    /// </summary>
    /// <param name="request">The transaction creation payload.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The created transaction.</returns>
    Task<Transaction> CreateTransactionAsync(CreateTransactionRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a draft transaction with a single signer.
    /// </summary>
    /// <param name="signer">The signer to include in the draft transaction.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The created draft transaction.</returns>
    Task<Transaction> CreateDraftTransactionAsync(SignerInput signer, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a document to an existing transaction.
    /// </summary>
    /// <param name="transactionId">The transaction identifier.</param>
    /// <param name="request">The document request payload.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The updated transaction.</returns>
    Task<Transaction> AddDocumentAsync(ProofTransactionId transactionId, AddDocumentRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a transaction by identifier.
    /// </summary>
    /// <param name="transactionId">The transaction identifier.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The transaction payload returned by Proof.</returns>
    Task<Transaction> GetTransactionAsync(ProofTransactionId transactionId, CancellationToken cancellationToken = default);
}
