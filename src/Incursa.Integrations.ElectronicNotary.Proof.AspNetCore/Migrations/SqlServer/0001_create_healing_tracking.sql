IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'proof_healing')
BEGIN
    EXEC('CREATE SCHEMA [proof_healing]');
END;
GO

IF OBJECT_ID(N'[proof_healing].[tracked_transactions]', N'U') IS NULL
BEGIN
    CREATE TABLE [proof_healing].[tracked_transactions]
    (
        [transaction_id] NVARCHAR(128) NOT NULL PRIMARY KEY,
        [created_at_utc] DATETIMEOFFSET NOT NULL,
        [updated_at_utc] DATETIMEOFFSET NOT NULL,
        [next_poll_at_utc] DATETIMEOFFSET NULL,
        [last_polled_at_utc] DATETIMEOFFSET NULL,
        [lease_until_utc] DATETIMEOFFSET NULL,
        [completed_at_utc] DATETIMEOFFSET NULL,
        [last_status] NVARCHAR(128) NULL,
        [last_error] NVARCHAR(1024) NULL,
        [attempt_count] INT NOT NULL CONSTRAINT [DF_proof_healing_attempt_count] DEFAULT (0),
        [failure_count] INT NOT NULL CONSTRAINT [DF_proof_healing_failure_count] DEFAULT (0)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_proof_healing_due_poll' AND object_id = OBJECT_ID(N'[proof_healing].[tracked_transactions]'))
BEGIN
    CREATE INDEX [IX_proof_healing_due_poll]
        ON [proof_healing].[tracked_transactions] ([next_poll_at_utc])
        WHERE [completed_at_utc] IS NULL;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_proof_healing_lease' AND object_id = OBJECT_ID(N'[proof_healing].[tracked_transactions]'))
BEGIN
    CREATE INDEX [IX_proof_healing_lease]
        ON [proof_healing].[tracked_transactions] ([lease_until_utc]);
END;
GO
