ALTER TABLE IF EXISTS proof_healing.tracked_transactions
    ADD COLUMN IF NOT EXISTS quarantine_until_utc TIMESTAMPTZ NULL;
