# Incursa.Platform.Storage.Tests

Unit tests for the shared `Incursa.Platform.Storage` public contract.

## What belongs here

- Contract tests for key primitives, partition query rules, write-condition rules, and batch intent validation.
- Behavioral tests for shared result and exception types that callers depend on directly.

## What does not belong here

- Provider-specific Azure, SQL, or emulator-backed tests.
- Tests that depend on network access, external services, or SDK internals.

## Intended usage

Run these tests as part of the fast non-integration suite to keep the shared storage contract stable while providers evolve underneath it.

## Guarantees and non-goals

- These tests lock the public contract shape and the semantics that every provider must honor.
- They do not attempt provider conformance against external infrastructure; that belongs in provider-specific test projects.
