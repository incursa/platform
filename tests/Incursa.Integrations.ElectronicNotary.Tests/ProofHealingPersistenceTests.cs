namespace Incursa.Integrations.ElectronicNotary.Tests;

using FluentAssertions;
using Incursa.Integrations.ElectronicNotary.Proof;
using Incursa.Integrations.ElectronicNotary.Proof.AspNetCore;
using Incursa.Integrations.ElectronicNotary.Proof.AspNetCore.Persistence;
using Microsoft.Extensions.DependencyInjection;

[TestClass]
public sealed class ProofHealingPersistenceTests
{
    [TestMethod]
    public void ClaimSqlUsesSqlServerLockHintsWhenSqlServerProvider()
    {
        string sql = ProofHealingSql.BuildClaimDueSql(ProofHealingDatabaseProvider.SqlServer);

        sql.Should().Contain("READPAST");
        sql.Should().Contain("UPDLOCK");
        sql.Should().Contain("SYSUTCDATETIME()");
        sql.Should().Contain("quarantine_until_utc");
    }

    [TestMethod]
    public void ClaimSqlUsesSkipLockedWhenPostgresProvider()
    {
        string sql = ProofHealingSql.BuildClaimDueSql(ProofHealingDatabaseProvider.Postgres);

        sql.Should().Contain("FOR UPDATE SKIP LOCKED");
        sql.Should().Contain("now()");
        sql.Should().Contain("quarantine_until_utc");
    }

    [TestMethod]
    public void RecordFailureSqlUsesQuarantineThresholdWhenSqlServerProvider()
    {
        string sql = ProofHealingSql.BuildRecordFailureSql(ProofHealingDatabaseProvider.SqlServer);

        sql.Should().Contain("@quarantine_failure_count");
        sql.Should().Contain("@quarantine_duration_seconds");
        sql.Should().Contain("failure_count + 1");
    }

    [TestMethod]
    public void RecordFailureSqlUsesQuarantineThresholdWhenPostgresProvider()
    {
        string sql = ProofHealingSql.BuildRecordFailureSql(ProofHealingDatabaseProvider.Postgres);

        sql.Should().Contain("@quarantine_failure_count");
        sql.Should().Contain("@quarantine_duration_seconds");
        sql.Should().Contain("failure_count + 1");
    }

    [TestMethod]
    public void AddProofHealingPersistenceReplacesNoOpHealingSourceWithDatabaseStore()
    {
        ServiceCollection services = new ServiceCollection();
        services.AddLogging();
        services.AddProofHealing();
        services.AddProofHealingPersistence(options =>
        {
            options.DatabaseProvider = ProofHealingDatabaseProvider.Postgres;
            options.ConnectionString = "Host=localhost;Database=proof;Username=user;Password=pwd;";
        });

        using ServiceProvider serviceProvider = services.BuildServiceProvider();

        IProofHealingTransactionSource source = serviceProvider.GetRequiredService<IProofHealingTransactionSource>();
        source.Should().BeOfType<ProofHealingDatabaseStore>();

        IProofHealingTransactionRegistry registry = serviceProvider.GetRequiredService<IProofHealingTransactionRegistry>();
        registry.Should().BeOfType<ProofHealingDatabaseStore>();

        IProofTransactionRegistrationSink registrationSink = serviceProvider.GetRequiredService<IProofTransactionRegistrationSink>();
        registrationSink.Should().BeOfType<ProofHealingTransactionRegistrationSink>();

        IReadOnlyCollection<IProofHealingObserver> observers = serviceProvider.GetServices<IProofHealingObserver>().ToArray();
        observers.Should().ContainSingle(static observer => observer is ProofHealingDatabaseStore);
    }
}
