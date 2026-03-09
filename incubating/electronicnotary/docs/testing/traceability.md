# Proof Traceability Matrix

The table maps scenario IDs in `docs/specs/` to automated tests.

| ScenarioId | Status | Test | File | Notes |
| --- | --- | --- | --- | --- |
| `PRF-CLI-001` | covered | `AddProofClientUsesFairfaxAndApiKeyHeaderAsync` | `tests/Incursa.Integrations.ElectronicNotary.Tests/ProofClientRegistrationTests.cs` | Environment routing |
| `PRF-CLI-002` | covered | `AddProofClientPrefersExplicitBaseUrlAsync` | `tests/Incursa.Integrations.ElectronicNotary.Tests/ProofClientRegistrationTests.cs` | Explicit URL override |
| `PRF-CLI-003` | covered | `AddProofClientThrowsWhenApiKeyMissing` | `tests/Incursa.Integrations.ElectronicNotary.Tests/ProofClientRegistrationTests.cs` | Config guard |
| `PRF-CLI-004` | covered | `AddProofClientThrowsWhenTimeoutNotPositive` | `tests/Incursa.Integrations.ElectronicNotary.Tests/ProofClientRegistrationTests.cs` | Config guard |
| `PRF-CLI-005` | covered | `CreateTransactionRegistersTransactionIdWithSinkAsync` | `tests/Incursa.Integrations.ElectronicNotary.Tests/ProofClientHttpTests.cs` | Registration sink behavior |
| `PRF-CLI-006` | covered | `NonSuccessResponseThrowsProofApiExceptionWithCorrelationAsync` | `tests/Incursa.Integrations.ElectronicNotary.Tests/ProofClientHttpTests.cs` | Error contract |
| `PRF-CLI-007` | covered | `GetTransactionRetriesTransientStatusCodesAsync` | `tests/Incursa.Integrations.ElectronicNotary.Tests/ProofClientHttpTests.cs` | Retry behavior |
| `PRF-CLI-008` | covered | `CircuitBreakerRejectsRequestsAfterConsecutiveTransientFailuresAsync` | `tests/Incursa.Integrations.ElectronicNotary.Tests/ProofClientHttpTests.cs` | Circuit behavior |
| `PRF-CLI-009` | planned | `CreateTransactionDoesNotRetryUnsafeMethodsByDefaultAsync` | `tests/Incursa.Integrations.ElectronicNotary.Tests/ProofClientHttpTests.cs` | Add explicit unsafe-method assertion |
| `PRF-CLI-010` | planned | `EmptyPayloadThrowsInvalidOperationExceptionAsync` | `tests/Incursa.Integrations.ElectronicNotary.Tests/ProofClientHttpTests.cs` | Add explicit empty payload assertion |
| `PRF-WEB-001` | covered | `InvalidSignatureIsRejectedAsync` | `tests/Incursa.Integrations.ElectronicNotary.Tests/ProofWebhookEndpointTests.cs` | Default signature requirement |
| `PRF-WEB-002` | covered | `ValidWebhookReturns200AndIsEnqueuedAsync` | `tests/Incursa.Integrations.ElectronicNotary.Tests/ProofWebhookEndpointTests.cs` | Accept path |
| `PRF-WEB-003` | covered | `AuthenticateAcceptsBearerTokenWhenConfiguredAsync` | `tests/Incursa.Integrations.ElectronicNotary.Tests/ProofWebhookAuthenticatorTests.cs` | Bearer auth |
| `PRF-WEB-004` | covered | `ClassifierUsesStableKeyFromEventTransactionAndDateAsync` | `tests/Incursa.Integrations.ElectronicNotary.Tests/ProofWebhookClassifierTests.cs` | Deterministic dedupe |
| `PRF-WEB-005` | covered | `ClassifierFallsBackToBodyHashWhenTransactionOrDateMissingAsync` | `tests/Incursa.Integrations.ElectronicNotary.Tests/ProofWebhookClassifierTests.cs` | Hash fallback |
| `PRF-WEB-006` | covered | `KnownCompletedEventDispatchesTypedDtoAsync` | `tests/Incursa.Integrations.ElectronicNotary.Tests/ProofWebhookDispatchHandlerTests.cs` | Typed dispatch |
| `PRF-WEB-007` | covered | `UnknownEventDispatchesUnknownEnvelopeAsync` | `tests/Incursa.Integrations.ElectronicNotary.Tests/ProofWebhookDispatchHandlerTests.cs` | Unknown fallback |
| `PRF-WEB-008` | covered | `KnownCompletedEventDispatchesTypedDtoAsync` | `tests/Incursa.Integrations.ElectronicNotary.Tests/ProofWebhookDispatchHandlerTests.cs` | Terminal marking in dispatch |
| `PRF-WEB-009` | covered | `InvalidSignatureIsRejectedAndNotProcessedAsync` | `tests/Incursa.Integrations.ElectronicNotary.Tests/ProofWebhookAspNetCoreIntegrationTests.cs` | End-to-end rejection |
| `PRF-WEB-010` | covered | `SignedWebhookIsAcceptedThenProcessedAndDedupedAsync` | `tests/Incursa.Integrations.ElectronicNotary.Tests/ProofWebhookAspNetCoreIntegrationTests.cs` | Replay safety |
| `PRF-HEAL-001` | covered | `RunOnceSkipsPollingWhenDisabledAsync` | `tests/Incursa.Integrations.ElectronicNotary.Tests/ProofHealingHostedServiceTests.cs` | Disabled behavior |
| `PRF-HEAL-002` | covered | `RunOncePollsTransactionsAndNotifiesObserversAsync` | `tests/Incursa.Integrations.ElectronicNotary.Tests/ProofHealingHostedServiceTests.cs` | Enabled behavior |
| `PRF-HEAL-003` | covered | `ClaimSqlUsesSqlServerLockHintsWhenSqlServerProvider` | `tests/Incursa.Integrations.ElectronicNotary.Tests/ProofHealingPersistenceTests.cs` | SQL Server lease semantics |
| `PRF-HEAL-004` | covered | `RecordFailureSqlUsesQuarantineThresholdWhenSqlServerProvider` | `tests/Incursa.Integrations.ElectronicNotary.Tests/ProofHealingPersistenceTests.cs` | Failure handling SQL |
| `PRF-HEAL-005` | covered | `RecordFailureSqlUsesQuarantineThresholdWhenPostgresProvider` | `tests/Incursa.Integrations.ElectronicNotary.Tests/ProofHealingPersistenceTests.cs` | Quarantine semantics |
| `PRF-HEAL-006` | covered | `ClaimSqlUsesSkipLockedWhenPostgresProvider` | `tests/Incursa.Integrations.ElectronicNotary.Tests/ProofHealingPersistenceTests.cs` | Quarantine-aware claim SQL |
| `PRF-HEAL-007` | covered | `KnownCompletedEventDispatchesTypedDtoAsync` | `tests/Incursa.Integrations.ElectronicNotary.Tests/ProofWebhookDispatchHandlerTests.cs` | Terminal webhook to registry |
| `PRF-HEAL-008` | planned | `MigrationLockTimeoutIsAppliedAsync` | `tests/Incursa.Integrations.ElectronicNotary.Tests/ProofHealingPersistenceTests.cs` | Add lock timeout behavior test |
| `PRF-HEAL-009` | planned | `JitteredIntervalsStayWithinBoundsAsync` | `tests/Incursa.Integrations.ElectronicNotary.Tests/ProofHealingPersistenceTests.cs` | Add jitter bound assertions |
| `PRF-HEAL-010` | covered | `RunOnceSkipsPollingWhenDisabledAsync` | `tests/Incursa.Integrations.ElectronicNotary.Tests/ProofHealingHostedServiceTests.cs` | Observed no-client skip path to add dedicated assertion later |
