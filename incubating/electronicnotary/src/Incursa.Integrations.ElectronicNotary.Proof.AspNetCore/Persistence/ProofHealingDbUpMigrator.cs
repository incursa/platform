namespace Incursa.Integrations.ElectronicNotary.Proof.AspNetCore.Persistence;

using System.Reflection;
using DbUp;
using DbUp.Engine;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

internal sealed partial class ProofHealingDbUpMigrator : IDisposable
{
    private readonly IOptions<ProofHealingPersistenceOptions> options;
    private readonly ILogger<ProofHealingDbUpMigrator> logger;
    private readonly SemaphoreSlim gate = new SemaphoreSlim(1, 1);
    private volatile bool isMigrated;

    public ProofHealingDbUpMigrator(
        IOptions<ProofHealingPersistenceOptions> options,
        ILogger<ProofHealingDbUpMigrator> logger)
    {
        this.options = options;
        this.logger = logger;
    }

    public async Task EnsureMigratedAsync(CancellationToken cancellationToken)
    {
        if (this.isMigrated)
        {
            return;
        }

        ProofHealingPersistenceOptions currentOptions = this.options.Value;
        TimeSpan waitTimeout = currentOptions.MigrationLockTimeout <= TimeSpan.Zero
            ? TimeSpan.FromSeconds(30)
            : currentOptions.MigrationLockTimeout;
        bool entered = await this.gate.WaitAsync(waitTimeout, cancellationToken).ConfigureAwait(false);
        if (!entered)
        {
            throw new TimeoutException($"Timed out acquiring Proof healing migration lock after '{waitTimeout}'.");
        }

        try
        {
            if (this.isMigrated)
            {
                return;
            }

            ProofHealingPersistenceOptions current = this.options.Value;
            if (!current.Enabled || !current.ApplyMigrationsAtStartup || string.IsNullOrWhiteSpace(current.ConnectionString))
            {
                this.isMigrated = true;
                return;
            }

            UpgradeEngine engine = CreateUpgradeEngine(current);
            DatabaseUpgradeResult result = await Task.Run(engine.PerformUpgrade, cancellationToken).ConfigureAwait(false);

            if (!result.Successful)
            {
                throw new InvalidOperationException("Proof healing schema migration failed.", result.Error);
            }

            LogMigrationCompleted(this.logger);
            this.isMigrated = true;
        }
        finally
        {
            this.gate.Release();
        }
    }

    public void Dispose()
    {
        this.gate.Dispose();
    }

    private static UpgradeEngine CreateUpgradeEngine(ProofHealingPersistenceOptions current)
    {
        return current.DatabaseProvider switch
        {
            ProofHealingDatabaseProvider.SqlServer =>
                DeployChanges.To
                    .SqlDatabase(current.ConnectionString!)
                    .WithScriptsEmbeddedInAssembly(
                        Assembly.GetExecutingAssembly(),
                        static resourceName => resourceName.Contains(".Migrations.SqlServer.", StringComparison.Ordinal))
                    .LogToNowhere()
                    .Build(),
            ProofHealingDatabaseProvider.Postgres =>
                DeployChanges.To
                    .PostgresqlDatabase(current.ConnectionString!)
                    .WithScriptsEmbeddedInAssembly(
                        Assembly.GetExecutingAssembly(),
                        static resourceName => resourceName.Contains(".Migrations.Postgres.", StringComparison.Ordinal))
                    .LogToNowhere()
                    .Build(),
            _ => throw new InvalidOperationException("Unsupported healing database provider."),
        };
    }

    [LoggerMessage(EventId = 1101, Level = LogLevel.Information, Message = "Proof healing schema migration completed successfully.")]
    private static partial void LogMigrationCompleted(ILogger logger);
}
