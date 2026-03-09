namespace Incursa.Integrations.ElectronicNotary.Proof.AspNetCore;

/// <summary>
/// Configures background healing polling for missed or delayed webhook events.
/// </summary>
public sealed class ProofHealingOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether healing polling is enabled.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the delay between healing polling cycles.
    /// </summary>
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets or sets the maximum number of transactions to poll per cycle.
    /// </summary>
    public int MaxTransactionsPerCycle { get; set; } = 100;

    /// <summary>
    /// Gets or sets the target delay between successful polls for an active transaction.
    /// </summary>
    public TimeSpan TransactionPollInterval { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Gets or sets the retry delay used after a polling failure.
    /// </summary>
    public TimeSpan FailureRetryDelay { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Gets or sets the number of consecutive polling failures required to quarantine a transaction.
    /// </summary>
    public int MaxFailureCountBeforeQuarantine { get; set; } = 6;

    /// <summary>
    /// Gets or sets the duration a transaction remains quarantined after exceeding failure limits.
    /// </summary>
    public TimeSpan QuarantineDuration { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Gets or sets a value from 0 to 1 that controls delay jitter to reduce synchronized polling spikes.
    /// </summary>
    public double JitterPercent { get; set; } = 0.1d;

    /// <summary>
    /// Gets or sets the duration for row-claim leases when selecting due transactions.
    /// </summary>
    public TimeSpan ClaimLeaseDuration { get; set; } = TimeSpan.FromMinutes(2);
}
