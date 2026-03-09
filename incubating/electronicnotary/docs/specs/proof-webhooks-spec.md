# Proof Webhook Behavior Specification

This document defines expected behavior for webhook authentication, classification, and dispatch.

## Scenarios

- `PRF-WEB-001`: Signature validation is required by default when bearer token authentication is not used.
- `PRF-WEB-002`: A valid signature results in accepted ingestion and enqueue.
- `PRF-WEB-003`: A configured bearer token authenticates webhook requests without signature verification.
- `PRF-WEB-004`: Classifier dedupe key uses provider + event + transaction ID + date when available.
- `PRF-WEB-005`: Classifier falls back to stable body-hash dedupe when transaction/date are unavailable.
- `PRF-WEB-006`: Known typed events are dispatched to matching typed handler methods.
- `PRF-WEB-007`: Unknown events are dispatched to `OnUnknownAsync`.
- `PRF-WEB-008`: Terminal events mark tracked transactions terminal in healing registry.
- `PRF-WEB-009`: Invalid signatures are rejected and never processed.
- `PRF-WEB-010`: Duplicate webhook deliveries are idempotently de-duplicated by ingestion pipeline.
