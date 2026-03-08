# Incursa.Integrations.Storage.Azure.Tests

This project verifies the Azure-backed storage provider for `Incursa.Platform.Storage`.

What belongs here:
- Unit tests for Azure-specific name mapping, options validation, serialization, metadata mapping, queue envelopes, and DI registration.
- Contract behavior tests for clearly unsupported operations and optimistic-concurrency guard rails.
- Opt-in Azurite integration tests for blob, queue, and table-backed paths.

What does not belong here:
- Production SDK wrappers or implementation code.
- Live-cloud tests that require a real Azure subscription.
- Domain-specific storage behavior for billing, tenants, or organization concepts.

Intended usage:
- Run as part of normal test execution for unit coverage.
- Run integration tests explicitly when an Azurite connection string is available.

Guarantees and non-goals:
- Unit tests verify provider-specific behavior without leaking Azure SDK types through the public platform API.
- Integration tests validate real blob, queue, and optional table paths against Azurite when configured.
- These tests do not try to prove cross-partition transactions or full Azure service parity beyond the supported contract.
