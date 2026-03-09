# Proof Client Behavior Specification

This document defines expected behavior for `Incursa.Integrations.ElectronicNotary.Proof` client operations.

## Scenarios

- `PRF-CLI-001`: When `Environment=Fairfax` and `BaseUrl` is unset, requests use `https://api.fairfax.proof.com/`.
- `PRF-CLI-002`: When an explicit absolute `BaseUrl` is configured, it overrides `Environment`.
- `PRF-CLI-003`: When `ApiKey` is missing/empty, client resolution fails with configuration error.
- `PRF-CLI-004`: When `Timeout <= 0`, client resolution fails with configuration error.
- `PRF-CLI-005`: `CreateTransactionAsync` posts expected payload and registers created transaction ID in the registration sink.
- `PRF-CLI-006`: Non-success API responses throw `ProofApiException` containing status, body, and correlation info when present.
- `PRF-CLI-007`: For configured transient status codes, idempotent methods retry with bounded backoff and jitter.
- `PRF-CLI-008`: Circuit opens after configured consecutive transient failures and rejects requests until break duration elapses.
- `PRF-CLI-009`: Unsafe methods are not retried unless `RetryUnsafeMethods=true`.
- `PRF-CLI-010`: Successful responses with empty transaction payload throw `InvalidOperationException`.
