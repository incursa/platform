namespace Incursa.Integrations.ElectronicNotary.Proof.AspNetCore;

using Incursa.Integrations.ElectronicNotary.Proof;
using Incursa.Integrations.ElectronicNotary.Proof.Types;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

internal sealed partial class ProofHealingHostedService : BackgroundService
{
    private readonly IServiceProvider serviceProvider;
    private readonly IProofHealingTransactionSource transactionSource;
    private readonly IProofHealingTransactionRegistry transactionRegistry;
    private readonly IReadOnlyList<IProofHealingObserver> observers;
    private readonly IOptions<ProofHealingOptions> options;
    private readonly ILogger<ProofHealingHostedService> logger;

    public ProofHealingHostedService(
        IServiceProvider serviceProvider,
        IProofHealingTransactionSource transactionSource,
        IProofHealingTransactionRegistry transactionRegistry,
        IEnumerable<IProofHealingObserver> observers,
        IOptions<ProofHealingOptions> options,
        ILogger<ProofHealingHostedService> logger)
    {
        this.serviceProvider = serviceProvider;
        this.transactionSource = transactionSource;
        this.transactionRegistry = transactionRegistry;
        this.observers = observers.ToArray();
        this.options = options;
        this.logger = logger;
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Healing polling isolates per-transaction failures to keep the background loop progressing.")]
    internal async Task<int> RunOnceAsync(CancellationToken cancellationToken)
    {
        ProofHealingOptions current = this.options.Value;
        if (!current.Enabled)
        {
            return 0;
        }

        int maxCount = Math.Max(1, current.MaxTransactionsPerCycle);
        IReadOnlyCollection<ProofTransactionId> ids = await this.transactionSource
            .GetTransactionIdsToPollAsync(maxCount, cancellationToken)
            .ConfigureAwait(false);
        IProofClient? proofClient = this.serviceProvider.GetService(typeof(IProofClient)) as IProofClient;
        if (proofClient is null)
        {
            LogNoProofClient(this.logger);
            return 0;
        }

        int processed = 0;
        foreach (ProofTransactionId transactionId in ids.Where(static id => !string.IsNullOrWhiteSpace(id.Value)).Distinct().Take(maxCount))
        {
            try
            {
                var transaction = await proofClient.GetTransactionAsync(transactionId, cancellationToken).ConfigureAwait(false);
                foreach (IProofHealingObserver observer in this.observers)
                {
                    await observer.OnTransactionPolledAsync(transactionId, transaction, cancellationToken).ConfigureAwait(false);
                }

                processed++;
            }
            catch (Exception exception)
            {
                await this.transactionRegistry
                    .RecordPollingFailureAsync(transactionId, exception.Message, cancellationToken)
                    .ConfigureAwait(false);
                LogPollingFailure(this.logger, exception, transactionId.Value);
            }
        }

        return processed;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        ProofHealingOptions current = this.options.Value;
        if (!current.Enabled)
        {
            return;
        }

        TimeSpan interval = ValidatePollInterval(current.PollInterval);
        using var timer = new PeriodicTimer(interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            await this.RunOnceAsync(stoppingToken).ConfigureAwait(false);
            await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false);
        }
    }

    private static TimeSpan ValidatePollInterval(TimeSpan interval)
    {
        if (interval <= TimeSpan.Zero)
        {
            throw new InvalidOperationException("Proof healing poll interval must be greater than zero.");
        }

        return interval;
    }

    [LoggerMessage(EventId = 1001, Level = LogLevel.Debug, Message = "Proof healing is enabled, but no IProofClient is registered. Skipping cycle.")]
    private static partial void LogNoProofClient(ILogger logger);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Warning, Message = "Proof healing poll failed for transaction '{TransactionId}'.")]
    private static partial void LogPollingFailure(ILogger logger, Exception exception, string transactionId);
}
