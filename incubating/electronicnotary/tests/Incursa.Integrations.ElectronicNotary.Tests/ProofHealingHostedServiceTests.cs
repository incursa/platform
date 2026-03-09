namespace Incursa.Integrations.ElectronicNotary.Tests;

using FluentAssertions;
using Incursa.Integrations.ElectronicNotary.Proof;
using Incursa.Integrations.ElectronicNotary.Proof.AspNetCore;
using Incursa.Integrations.ElectronicNotary.Proof.Contracts;
using Incursa.Integrations.ElectronicNotary.Proof.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

[TestClass]
public sealed class ProofHealingHostedServiceTests
{
    [TestMethod]
    public async Task RunOncePollsTransactionsAndNotifiesObserversAsync()
    {
        var source = new StaticTransactionSource(
            new[]
            {
                ProofTransactionId.Parse("ot_heal001"),
                ProofTransactionId.Parse("ot_heal001"),
                ProofTransactionId.Parse("ot_heal002"),
            });
        var client = new RecordingProofClient();
        var observer = new RecordingHealingObserver();
        using ServiceProvider serviceProvider = new ServiceCollection()
            .AddSingleton<IProofClient>(client)
            .BuildServiceProvider();
        using var service = new ProofHealingHostedService(
            serviceProvider,
            source,
            new NoOpProofHealingTransactionRegistry(),
            new[] { observer },
            Options.Create(new ProofHealingOptions
            {
                Enabled = true,
                MaxTransactionsPerCycle = 10,
                PollInterval = TimeSpan.FromMinutes(1),
            }),
            NullLogger<ProofHealingHostedService>.Instance);

        int processed = await service.RunOnceAsync(CancellationToken.None).ConfigureAwait(false);

        processed.Should().Be(2);
        client.PolledTransactionIds.Should().BeEquivalentTo(
            new[]
            {
                ProofTransactionId.Parse("ot_heal001"),
                ProofTransactionId.Parse("ot_heal002"),
            });
        observer.ObservedTransactionIds.Should().BeEquivalentTo(client.PolledTransactionIds);
    }

    [TestMethod]
    public async Task RunOnceSkipsPollingWhenDisabledAsync()
    {
        var source = new StaticTransactionSource(
            new[]
            {
                ProofTransactionId.Parse("ot_heal001"),
            });
        var client = new RecordingProofClient();
        var observer = new RecordingHealingObserver();
        using ServiceProvider serviceProvider = new ServiceCollection()
            .AddSingleton<IProofClient>(client)
            .BuildServiceProvider();
        using var service = new ProofHealingHostedService(
            serviceProvider,
            source,
            new NoOpProofHealingTransactionRegistry(),
            new[] { observer },
            Options.Create(new ProofHealingOptions
            {
                Enabled = false,
                PollInterval = TimeSpan.FromMinutes(1),
            }),
            NullLogger<ProofHealingHostedService>.Instance);

        int processed = await service.RunOnceAsync(CancellationToken.None).ConfigureAwait(false);

        processed.Should().Be(0);
        client.PolledTransactionIds.Should().BeEmpty();
        observer.ObservedTransactionIds.Should().BeEmpty();
    }

    private sealed class StaticTransactionSource : IProofHealingTransactionSource
    {
        private readonly IReadOnlyCollection<ProofTransactionId> ids;

        public StaticTransactionSource(IReadOnlyCollection<ProofTransactionId> ids)
        {
            this.ids = ids;
        }

        public Task<IReadOnlyCollection<ProofTransactionId>> GetTransactionIdsToPollAsync(int maxCount, CancellationToken cancellationToken)
        {
            return Task.FromResult(this.ids);
        }
    }

    private sealed class RecordingHealingObserver : IProofHealingObserver
    {
        public List<ProofTransactionId> ObservedTransactionIds { get; } = new List<ProofTransactionId>();

        public Task OnTransactionPolledAsync(ProofTransactionId transactionId, Transaction transaction, CancellationToken cancellationToken)
        {
            this.ObservedTransactionIds.Add(transactionId);
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingProofClient : IProofClient
    {
        public List<ProofTransactionId> PolledTransactionIds { get; } = new List<ProofTransactionId>();

        public Task<Transaction> CreateTransactionAsync(CreateTransactionRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<Transaction> CreateDraftTransactionAsync(SignerInput signer, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<Transaction> AddDocumentAsync(ProofTransactionId transactionId, AddDocumentRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<Transaction> GetTransactionAsync(ProofTransactionId transactionId, CancellationToken cancellationToken = default)
        {
            this.PolledTransactionIds.Add(transactionId);
            Transaction transaction = Transaction.Create(
                transactionId,
                null,
                "active",
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow);
            return Task.FromResult(transaction);
        }
    }
}
