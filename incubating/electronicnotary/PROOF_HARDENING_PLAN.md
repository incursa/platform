# Proof Integration Hardening Plan

## 1. Verification Strategy First
- Confirm whether Proof Fairfax/sandbox credentials are available; if yes, use them for live contract validation.
- If sandbox access is unavailable, stand up a deterministic fake Proof API (WireMock/TestServer) that covers success, validation failures, rate limits, and timeouts.

## 2. Expand the Test Pyramid
- Keep existing unit tests and add full client contract tests for each endpoint and error payload shape.
- Add webhook end-to-end tests for signature validation, deduplication, replay safety, out-of-order delivery, and malformed payloads.
- Add persistence tests against real SQL Server and PostgreSQL containers for healing migrations and data access.

## 3. Resilience Hardening
- Add outbound HTTP resiliency policies (retry/backoff/circuit-breaker) for Proof API calls.
- Add explicit handling for `429`, `5xx`, and timeout classes with bounded retries and telemetry.

## 4. Security Hardening
- Keep webhook signature verification required by default and test signing-key rotation.
- Add tests that verify API keys/signing keys are never logged and are always redacted in structured logs.

## 5. Healing Service Hardening
- Validate scheduler cadence and business-hours polling policy.
- Validate multi-instance behavior (lease/single-run guarantees) so two instances cannot process the same work item concurrently.
- Add race-condition tests where webhook completion and poller completion happen at nearly the same time.

## 6. Observability and Operations
- Add metrics for webhook accepted/rejected counts, dedupe rate, polling success/failure, Proof API latency, and terminal-state lag.
- Add health checks and operational runbooks for stuck queues, migration failures, and Proof API outages.

## 7. Release Gate
- Make CI gates enforce: build, analyzers, unit tests, integration tests, and migration smoke tests.
- Roll out in stages: canary environment, synthetic transactions, then controlled production enablement.

## Definition of Done for “Hardened”
- Deterministic automated coverage for critical success and failure paths.
- Proven idempotency and multi-instance safety under concurrent conditions.
- Documented runbooks and observable SLO-oriented telemetry in place.
