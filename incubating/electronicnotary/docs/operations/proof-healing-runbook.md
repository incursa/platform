# Proof Healing Operations Runbook

## Signals

- Poll success/failure counts.
- Quarantine count and oldest quarantine age.
- Terminal-state lag (transaction create -> terminal observed).
- Migration startup success/failure and lock timeout events.

## Common Incidents

### Growing Quarantine Backlog

- Inspect persisted `last_error` and failure counts.
- Verify Proof API health and outbound credentials.
- Re-drive only after underlying cause is fixed.

### Polling Stops Progressing

- Check `Enabled` options and hosted service registration.
- Validate DB connectivity and lease claim query behavior.
- Check if migration lock timeout is repeatedly failing at startup.

### Duplicate Poll Processing Across Nodes

- Verify SQL lease claim semantics are in use for selected provider.
- Confirm clock skew and lease duration settings.
- Audit deployment concurrency and instance startup patterns.
