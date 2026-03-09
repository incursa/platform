# Proof Webhook Operations Runbook

## Signals

- Webhook accepted/rejected counts.
- Signature failure rate.
- Dedupe hit ratio.
- Processing lag from ingest to handler completion.

## Common Incidents

### Elevated 401/Unauthorized

- Check signing key/bearer token configuration.
- Verify provider header name and request payload canonicalization.
- Confirm key rotation rollout order across environments.

### Duplicate Delivery Spike

- Validate dedupe key generation and storage availability.
- Confirm inbox persistence health.
- Verify no recent regression in classifier payload extraction.

### High Unknown Event Rate

- Inspect unknown event samples.
- Determine if new provider event types need typed contracts.
- Add/update dispatch handler coverage before enabling typed processing.
