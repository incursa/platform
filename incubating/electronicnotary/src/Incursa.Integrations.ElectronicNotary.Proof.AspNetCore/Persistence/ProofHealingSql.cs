namespace Incursa.Integrations.ElectronicNotary.Proof.AspNetCore.Persistence;

internal static class ProofHealingSql
{
    private const string QualifiedTableSqlServer =
        $"[{ProofHealingPersistenceConstants.SchemaName}].[{ProofHealingPersistenceConstants.TableName}]";

    private const string QualifiedTablePostgres =
        $"\"{ProofHealingPersistenceConstants.SchemaName}\".\"{ProofHealingPersistenceConstants.TableName}\"";

    public static string BuildClaimDueSql(ProofHealingDatabaseProvider provider)
    {
        return provider switch
        {
            ProofHealingDatabaseProvider.SqlServer => $"""
                ;WITH cte AS (
                    SELECT TOP (@max_count) transaction_id
                    FROM {QualifiedTableSqlServer} WITH (UPDLOCK, READPAST, ROWLOCK)
                    WHERE completed_at_utc IS NULL
                      AND next_poll_at_utc IS NOT NULL
                      AND next_poll_at_utc <= SYSUTCDATETIME()
                      AND (quarantine_until_utc IS NULL OR quarantine_until_utc <= SYSUTCDATETIME())
                      AND (lease_until_utc IS NULL OR lease_until_utc <= SYSUTCDATETIME())
                    ORDER BY next_poll_at_utc ASC
                )
                UPDATE cte
                SET lease_until_utc = DATEADD(second, @lease_seconds, SYSUTCDATETIME()),
                    updated_at_utc = SYSUTCDATETIME()
                OUTPUT inserted.transaction_id;
                """,
            ProofHealingDatabaseProvider.Postgres => $"""
                WITH cte AS (
                    SELECT transaction_id
                    FROM {QualifiedTablePostgres}
                    WHERE completed_at_utc IS NULL
                      AND next_poll_at_utc IS NOT NULL
                      AND next_poll_at_utc <= now()
                      AND (quarantine_until_utc IS NULL OR quarantine_until_utc <= now())
                      AND (lease_until_utc IS NULL OR lease_until_utc <= now())
                    ORDER BY next_poll_at_utc ASC
                    LIMIT @max_count
                    FOR UPDATE SKIP LOCKED
                )
                UPDATE {QualifiedTablePostgres} AS tracked
                SET lease_until_utc = now() + make_interval(secs => @lease_seconds),
                    updated_at_utc = now()
                FROM cte
                WHERE tracked.transaction_id = cte.transaction_id
                RETURNING tracked.transaction_id;
                """,
            _ => throw new InvalidOperationException("Unsupported healing database provider."),
        };
    }

    public static string BuildRegisterSql(ProofHealingDatabaseProvider provider)
    {
        return provider switch
        {
            ProofHealingDatabaseProvider.SqlServer => $"""
                UPDATE {QualifiedTableSqlServer}
                SET next_poll_at_utc = COALESCE(next_poll_at_utc, DATEADD(second, @initial_delay_seconds, SYSUTCDATETIME())),
                    updated_at_utc = SYSUTCDATETIME()
                WHERE transaction_id = @transaction_id;

                IF @@ROWCOUNT = 0
                BEGIN
                    INSERT INTO {QualifiedTableSqlServer}
                    (
                        transaction_id,
                        created_at_utc,
                        updated_at_utc,
                        next_poll_at_utc,
                        attempt_count,
                        failure_count
                    )
                    VALUES
                    (
                        @transaction_id,
                        SYSUTCDATETIME(),
                        SYSUTCDATETIME(),
                        DATEADD(second, @initial_delay_seconds, SYSUTCDATETIME()),
                        0,
                        0
                    );
                END;
                """,
            ProofHealingDatabaseProvider.Postgres => $"""
                INSERT INTO {QualifiedTablePostgres}
                (
                    transaction_id,
                    created_at_utc,
                    updated_at_utc,
                    next_poll_at_utc,
                    attempt_count,
                    failure_count
                )
                VALUES
                (
                    @transaction_id,
                    now(),
                    now(),
                    now() + make_interval(secs => @initial_delay_seconds),
                    0,
                    0
                )
                ON CONFLICT (transaction_id)
                DO UPDATE
                SET next_poll_at_utc = COALESCE({QualifiedTablePostgres}.next_poll_at_utc, now() + make_interval(secs => @initial_delay_seconds)),
                    updated_at_utc = now();
                """,
            _ => throw new InvalidOperationException("Unsupported healing database provider."),
        };
    }

    public static string BuildMarkTerminalSql(ProofHealingDatabaseProvider provider)
    {
        return provider switch
        {
            ProofHealingDatabaseProvider.SqlServer => $"""
                UPDATE {QualifiedTableSqlServer}
                SET completed_at_utc = @observed_at_utc,
                    last_status = @reason,
                    lease_until_utc = NULL,
                    quarantine_until_utc = NULL,
                    next_poll_at_utc = NULL,
                    updated_at_utc = SYSUTCDATETIME()
                WHERE transaction_id = @transaction_id;

                IF @@ROWCOUNT = 0
                BEGIN
                    INSERT INTO {QualifiedTableSqlServer}
                    (
                        transaction_id,
                        created_at_utc,
                        updated_at_utc,
                        next_poll_at_utc,
                        completed_at_utc,
                        last_status,
                        attempt_count,
                        failure_count
                    )
                    VALUES
                    (
                        @transaction_id,
                        SYSUTCDATETIME(),
                        SYSUTCDATETIME(),
                        NULL,
                        @observed_at_utc,
                        @reason,
                        0,
                        0
                    );
                END;
                """,
            ProofHealingDatabaseProvider.Postgres => $"""
                INSERT INTO {QualifiedTablePostgres}
                (
                    transaction_id,
                    created_at_utc,
                    updated_at_utc,
                    next_poll_at_utc,
                    completed_at_utc,
                    last_status,
                    attempt_count,
                    failure_count
                )
                VALUES
                (
                    @transaction_id,
                    now(),
                    now(),
                    NULL,
                    @observed_at_utc,
                    @reason,
                    0,
                    0
                )
                ON CONFLICT (transaction_id)
                DO UPDATE
                SET completed_at_utc = EXCLUDED.completed_at_utc,
                    last_status = EXCLUDED.last_status,
                    lease_until_utc = NULL,
                    quarantine_until_utc = NULL,
                    next_poll_at_utc = NULL,
                    updated_at_utc = now();
                """,
            _ => throw new InvalidOperationException("Unsupported healing database provider."),
        };
    }

    public static string BuildRecordSuccessSql(ProofHealingDatabaseProvider provider)
    {
        return provider switch
        {
            ProofHealingDatabaseProvider.SqlServer => $"""
                UPDATE {QualifiedTableSqlServer}
                SET last_status = @status,
                    last_polled_at_utc = SYSUTCDATETIME(),
                    attempt_count = attempt_count + 1,
                    failure_count = 0,
                    last_error = NULL,
                    lease_until_utc = NULL,
                    quarantine_until_utc = NULL,
                    next_poll_at_utc = CASE
                        WHEN @is_terminal = 1 THEN NULL
                        ELSE DATEADD(second, @next_poll_seconds, SYSUTCDATETIME())
                    END,
                    completed_at_utc = CASE
                        WHEN @is_terminal = 1 THEN COALESCE(completed_at_utc, SYSUTCDATETIME())
                        ELSE completed_at_utc
                    END,
                    updated_at_utc = SYSUTCDATETIME()
                WHERE transaction_id = @transaction_id;
                """,
            ProofHealingDatabaseProvider.Postgres => $"""
                UPDATE {QualifiedTablePostgres}
                SET last_status = @status,
                    last_polled_at_utc = now(),
                    attempt_count = attempt_count + 1,
                    failure_count = 0,
                    last_error = NULL,
                    lease_until_utc = NULL,
                    quarantine_until_utc = NULL,
                    next_poll_at_utc = CASE
                        WHEN @is_terminal = 1 THEN NULL
                        ELSE now() + make_interval(secs => @next_poll_seconds)
                    END,
                    completed_at_utc = CASE
                        WHEN @is_terminal = 1 THEN COALESCE(completed_at_utc, now())
                        ELSE completed_at_utc
                    END,
                    updated_at_utc = now()
                WHERE transaction_id = @transaction_id;
                """,
            _ => throw new InvalidOperationException("Unsupported healing database provider."),
        };
    }

    public static string BuildRecordFailureSql(ProofHealingDatabaseProvider provider)
    {
        return provider switch
        {
            ProofHealingDatabaseProvider.SqlServer => $"""
                UPDATE {QualifiedTableSqlServer}
                SET failure_count = failure_count + 1,
                    last_error = @error_message,
                    lease_until_utc = NULL,
                    quarantine_until_utc = CASE
                        WHEN failure_count + 1 >= @quarantine_failure_count THEN DATEADD(second, @quarantine_duration_seconds, SYSUTCDATETIME())
                        ELSE NULL
                    END,
                    next_poll_at_utc = CASE
                        WHEN failure_count + 1 >= @quarantine_failure_count THEN DATEADD(second, @quarantine_duration_seconds, SYSUTCDATETIME())
                        ELSE DATEADD(second, @retry_delay_seconds, SYSUTCDATETIME())
                    END,
                    updated_at_utc = SYSUTCDATETIME()
                WHERE transaction_id = @transaction_id;
                """,
            ProofHealingDatabaseProvider.Postgres => $"""
                UPDATE {QualifiedTablePostgres}
                SET failure_count = failure_count + 1,
                    last_error = @error_message,
                    lease_until_utc = NULL,
                    quarantine_until_utc = CASE
                        WHEN failure_count + 1 >= @quarantine_failure_count THEN now() + make_interval(secs => @quarantine_duration_seconds)
                        ELSE NULL
                    END,
                    next_poll_at_utc = CASE
                        WHEN failure_count + 1 >= @quarantine_failure_count THEN now() + make_interval(secs => @quarantine_duration_seconds)
                        ELSE now() + make_interval(secs => @retry_delay_seconds)
                    END,
                    updated_at_utc = now()
                WHERE transaction_id = @transaction_id;
                """,
            _ => throw new InvalidOperationException("Unsupported healing database provider."),
        };
    }
}
