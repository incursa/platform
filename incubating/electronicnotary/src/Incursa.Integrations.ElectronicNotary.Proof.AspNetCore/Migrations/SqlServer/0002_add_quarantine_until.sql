IF COL_LENGTH(N'[proof_healing].[tracked_transactions]', N'quarantine_until_utc') IS NULL
BEGIN
    ALTER TABLE [proof_healing].[tracked_transactions]
        ADD [quarantine_until_utc] DATETIMEOFFSET NULL;
END;
