CREATE SCHEMA IF NOT EXISTS proof_healing;

CREATE TABLE IF NOT EXISTS proof_healing.tracked_transactions
(
    transaction_id TEXT PRIMARY KEY,
    created_at_utc TIMESTAMPTZ NOT NULL,
    updated_at_utc TIMESTAMPTZ NOT NULL,
    next_poll_at_utc TIMESTAMPTZ NULL,
    last_polled_at_utc TIMESTAMPTZ NULL,
    lease_until_utc TIMESTAMPTZ NULL,
    completed_at_utc TIMESTAMPTZ NULL,
    last_status TEXT NULL,
    last_error TEXT NULL,
    attempt_count INTEGER NOT NULL DEFAULT 0,
    failure_count INTEGER NOT NULL DEFAULT 0
);

CREATE INDEX IF NOT EXISTS ix_proof_healing_due_poll
    ON proof_healing.tracked_transactions (next_poll_at_utc)
    WHERE completed_at_utc IS NULL;

CREATE INDEX IF NOT EXISTS ix_proof_healing_lease
    ON proof_healing.tracked_transactions (lease_until_utc);
