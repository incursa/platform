namespace Incursa.Integrations.ElectronicNotary.Proof.AspNetCore.Persistence;

using System.Data;
using System.Data.Common;
using System.Security.Cryptography;
using Incursa.Integrations.ElectronicNotary.Proof.Contracts;
using Incursa.Integrations.ElectronicNotary.Proof.Types;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

[SuppressMessage("Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Command text is selected from constant SQL templates and all variable values are parameterized.")]
internal sealed class ProofHealingDatabaseStore :
    IProofHealingTransactionSource,
    IProofHealingObserver,
    IProofHealingTransactionRegistry
{
    private static readonly string[] TerminalStatuses =
    [
        ProofTransactionDetailedStatus.CompleteValue,
        ProofTransactionDetailedStatus.CompleteWithRejectionsValue,
        ProofTransactionDetailedStatus.EsignCompleteValue,
        ProofTransactionDetailedStatus.WetSignCompleteValue,
        ProofTransactionDetailedStatus.ExpiredValue,
        ProofTransactionDetailedStatus.RecalledValue,
    ];

    private readonly IOptions<ProofHealingOptions> healingOptions;
    private readonly IOptions<ProofHealingPersistenceOptions> persistenceOptions;
    private readonly ProofHealingDbUpMigrator migrator;

    public ProofHealingDatabaseStore(
        IOptions<ProofHealingOptions> healingOptions,
        IOptions<ProofHealingPersistenceOptions> persistenceOptions,
        ProofHealingDbUpMigrator migrator,
        ILogger<ProofHealingDatabaseStore> logger)
    {
        this.healingOptions = healingOptions;
        this.persistenceOptions = persistenceOptions;
        this.migrator = migrator;
        _ = logger;
    }

    public async Task<IReadOnlyCollection<ProofTransactionId>> GetTransactionIdsToPollAsync(int maxCount, CancellationToken cancellationToken)
    {
        ProofHealingPersistenceOptions persistence = this.persistenceOptions.Value;
        if (!persistence.Enabled || string.IsNullOrWhiteSpace(persistence.ConnectionString))
        {
            return Array.Empty<ProofTransactionId>();
        }

        await this.migrator.EnsureMigratedAsync(cancellationToken).ConfigureAwait(false);

        List<ProofTransactionId> ids = new List<ProofTransactionId>();
        using DbConnection connection = this.CreateConnection(persistence);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        using DbCommand command = connection.CreateCommand();
        command.CommandText = ProofHealingSql.BuildClaimDueSql(persistence.DatabaseProvider);
        command.CommandType = CommandType.Text;
        this.AddParameter(command, persistence.DatabaseProvider, "@max_count", DbType.Int32, maxCount);
        this.AddParameter(
            command,
            persistence.DatabaseProvider,
            "@lease_seconds",
            DbType.Int32,
            (int)Math.Max(1, this.healingOptions.Value.ClaimLeaseDuration.TotalSeconds));

        using DbDataReader reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            string? idValue = reader.GetString(0);
            if (ProofTransactionId.TryParse(idValue, out ProofTransactionId id))
            {
                ids.Add(id);
            }
        }

        return ids;
    }

    public async Task RegisterTransactionAsync(ProofTransactionId transactionId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(transactionId.Value))
        {
            return;
        }

        ProofHealingPersistenceOptions persistence = this.persistenceOptions.Value;
        if (!persistence.Enabled || string.IsNullOrWhiteSpace(persistence.ConnectionString))
        {
            return;
        }

        await this.migrator.EnsureMigratedAsync(cancellationToken).ConfigureAwait(false);

        using DbConnection connection = this.CreateConnection(persistence);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        using DbCommand command = connection.CreateCommand();
        command.CommandText = ProofHealingSql.BuildRegisterSql(persistence.DatabaseProvider);
        this.AddParameter(command, persistence.DatabaseProvider, "@transaction_id", DbType.String, transactionId.Value);
        this.AddParameter(
            command,
            persistence.DatabaseProvider,
            "@initial_delay_seconds",
            DbType.Int32,
            this.ToJitteredSeconds(this.healingOptions.Value.TransactionPollInterval));
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task MarkTransactionTerminalAsync(
        ProofTransactionId transactionId,
        string reason,
        DateTimeOffset observedAtUtc,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(transactionId.Value))
        {
            return;
        }

        ProofHealingPersistenceOptions persistence = this.persistenceOptions.Value;
        if (!persistence.Enabled || string.IsNullOrWhiteSpace(persistence.ConnectionString))
        {
            return;
        }

        await this.migrator.EnsureMigratedAsync(cancellationToken).ConfigureAwait(false);

        using DbConnection connection = this.CreateConnection(persistence);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        using DbCommand command = connection.CreateCommand();
        command.CommandText = ProofHealingSql.BuildMarkTerminalSql(persistence.DatabaseProvider);
        this.AddParameter(command, persistence.DatabaseProvider, "@transaction_id", DbType.String, transactionId.Value);
        this.AddParameter(command, persistence.DatabaseProvider, "@reason", DbType.String, reason);
        this.AddParameter(command, persistence.DatabaseProvider, "@observed_at_utc", DbType.DateTimeOffset, observedAtUtc);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task RecordPollingFailureAsync(ProofTransactionId transactionId, string errorMessage, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(transactionId.Value))
        {
            return;
        }

        ProofHealingPersistenceOptions persistence = this.persistenceOptions.Value;
        if (!persistence.Enabled || string.IsNullOrWhiteSpace(persistence.ConnectionString))
        {
            return;
        }

        await this.migrator.EnsureMigratedAsync(cancellationToken).ConfigureAwait(false);

        using DbConnection connection = this.CreateConnection(persistence);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        using DbCommand command = connection.CreateCommand();
        command.CommandText = ProofHealingSql.BuildRecordFailureSql(persistence.DatabaseProvider);
        this.AddParameter(command, persistence.DatabaseProvider, "@transaction_id", DbType.String, transactionId.Value);
        this.AddParameter(command, persistence.DatabaseProvider, "@error_message", DbType.String, this.Truncate(errorMessage, 1024));
        int failureThreshold = this.healingOptions.Value.MaxFailureCountBeforeQuarantine <= 0
            ? int.MaxValue
            : this.healingOptions.Value.MaxFailureCountBeforeQuarantine;
        this.AddParameter(command, persistence.DatabaseProvider, "@quarantine_failure_count", DbType.Int32, failureThreshold);
        this.AddParameter(
            command,
            persistence.DatabaseProvider,
            "@quarantine_duration_seconds",
            DbType.Int32,
            (int)Math.Max(1, this.healingOptions.Value.QuarantineDuration.TotalSeconds));
        this.AddParameter(
            command,
            persistence.DatabaseProvider,
            "@retry_delay_seconds",
            DbType.Int32,
            this.ToJitteredSeconds(this.healingOptions.Value.FailureRetryDelay));
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task OnTransactionPolledAsync(ProofTransactionId transactionId, Transaction transaction, CancellationToken cancellationToken)
    {
        ProofHealingPersistenceOptions persistence = this.persistenceOptions.Value;
        if (!persistence.Enabled || string.IsNullOrWhiteSpace(persistence.ConnectionString))
        {
            return;
        }

        await this.migrator.EnsureMigratedAsync(cancellationToken).ConfigureAwait(false);

        string? detailedStatus = transaction.DetailedStatus?.Value;
        bool isTerminal = this.IsTerminalStatus(detailedStatus);

        using DbConnection connection = this.CreateConnection(persistence);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        using DbCommand command = connection.CreateCommand();
        command.CommandText = ProofHealingSql.BuildRecordSuccessSql(persistence.DatabaseProvider);
        this.AddParameter(command, persistence.DatabaseProvider, "@transaction_id", DbType.String, transactionId.Value);
        this.AddParameter(command, persistence.DatabaseProvider, "@status", DbType.String, detailedStatus ?? transaction.Status ?? string.Empty);
        this.AddParameter(command, persistence.DatabaseProvider, "@is_terminal", DbType.Int32, isTerminal ? 1 : 0);
        this.AddParameter(
            command,
            persistence.DatabaseProvider,
            "@next_poll_seconds",
            DbType.Int32,
            this.ToJitteredSeconds(this.healingOptions.Value.TransactionPollInterval));
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private bool IsTerminalStatus(string? status)
    {
        _ = this.healingOptions;
        if (string.IsNullOrWhiteSpace(status))
        {
            return false;
        }

        return TerminalStatuses.Contains(status, StringComparer.OrdinalIgnoreCase);
    }

    private string Truncate(string value, int maxLength)
    {
        _ = this.healingOptions;
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
        {
            return value;
        }

        return value.Substring(0, maxLength);
    }

    private int ToJitteredSeconds(TimeSpan baseDelay)
    {
        ProofHealingOptions options = this.healingOptions.Value;
        double jitterPercent = Math.Clamp(options.JitterPercent, 0d, 1d);
        double multiplier = 1d;
        if (jitterPercent > 0d)
        {
            double fraction = RandomNumberGenerator.GetInt32(0, 10001) / 10000d;
            double offset = (fraction * 2d * jitterPercent) - jitterPercent;
            multiplier += offset;
        }

        double seconds = Math.Max(1d, baseDelay.TotalSeconds * multiplier);
        return (int)Math.Ceiling(seconds);
    }

    private DbConnection CreateConnection(ProofHealingPersistenceOptions persistence)
    {
        _ = this.persistenceOptions;
        string connectionString = persistence.ConnectionString ?? throw new InvalidOperationException("Proof healing connection string is not configured.");
        return persistence.DatabaseProvider switch
        {
            ProofHealingDatabaseProvider.SqlServer => new SqlConnection(connectionString),
            ProofHealingDatabaseProvider.Postgres => new NpgsqlConnection(connectionString),
            _ => throw new InvalidOperationException("Unsupported healing database provider."),
        };
    }

    private void AddParameter(
        DbCommand command,
        ProofHealingDatabaseProvider provider,
        string name,
        DbType type,
        object? value)
    {
        _ = this.persistenceOptions;
        DbParameter parameter = provider switch
        {
            ProofHealingDatabaseProvider.SqlServer => new SqlParameter(),
            ProofHealingDatabaseProvider.Postgres => new NpgsqlParameter(),
            _ => throw new InvalidOperationException("Unsupported healing database provider."),
        };

        parameter.ParameterName = name;
        parameter.DbType = type;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }
}
