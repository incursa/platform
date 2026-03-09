# WorkOS Quality Traceability

## Scenario Mapping

| Scenario ID | Behavior | Test(s) |
| --- | --- | --- |
| AUTH-001 | Revoked API key is rejected | `WorkOsApiKeyAuthenticatorBehaviorTests.ValidateApiKeyAsync_Revoked_ReturnsRevoked` |
| AUTH-002 | Expired API key is rejected | `WorkOsApiKeyAuthenticatorBehaviorTests.ValidateApiKeyAsync_Expired_ReturnsExpired` |
| AUTH-003 | Strict unknown permission fails authorization | `WorkOsApiKeyAuthenticatorBehaviorTests.ValidateApiKeyAsync_StrictMappingWithUnknownPermission_ReturnsInsufficientScope` |
| AUTH-004 | Validation cache prevents duplicate remote call | `WorkOsApiKeyAuthenticatorBehaviorTests.ValidateApiKeyAsync_UsesCacheForSubsequentRequests` |
| WEBHOOK-001 | Missing signature is rejected | `WorkOsWebhookVerifierEdgeCaseTests.Verify_MissingSignatureHeader_ReturnsMissingSignature` |
| WEBHOOK-002 | Invalid signature format is rejected | `WorkOsWebhookVerifierEdgeCaseTests.Verify_InvalidSignatureHeaderFormat_ReturnsInvalidSignatureFormat` |
| WEBHOOK-003 | Signature mismatch is rejected | `WorkOsWebhookVerifierEdgeCaseTests.Verify_MismatchedSignature_ReturnsSignatureMismatch` |
| WEBHOOK-004 | First webhook delivery is processed | `WorkOsWebhookProcessorTests.ProcessAsync_FirstDelivery_InvokesHandlersAndReturnsProcessed` |
| WEBHOOK-005 | Duplicate webhook delivery is ignored | `WorkOsWebhookProcessorTests.ProcessAsync_DuplicateDelivery_ReturnsDuplicateWithoutInvokingHandlers` |
| WEBHOOK-006 | Handler faults bubble to caller | `WorkOsWebhookProcessorTests.ProcessAsync_HandlerThrows_BubblesException` |
| MGMT-001 | Validate payload parsing works | `WorkOsManagementHttpClientTests.ValidateApiKeyAsync_SuccessfulPayload_ParsesPermissions` |
| MGMT-002 | Create API key normalizes scopes and tenant mapping | `WorkOsManagementHttpClientTests.CreateApiKeyAsync_SetsNormalizedScopeOrderAndTenant` |
| MGMT-003 | Missing remote key returns null | `WorkOsManagementHttpClientTests.GetApiKeyAsync_NonSuccess_ReturnsNull` |
| FUZZ-001 | Randomized webhook signatures never crash verifier | `FuzzBehaviorTests.WorkOsWebhookVerifier_RandomizedHeadersAndBodies_DoesNotThrow` |
| FUZZ-002 | Randomized permission mapping remains stable ordered output | `FuzzBehaviorTests.WorkOsPermissionMapper_RandomizedPermissions_DoesNotThrowAndReturnsStableOrdering` |
| LIVE-001 | Staging validate flow returns a managed key | `WorkOsLiveIntegrationTests.ValidateApiKeyAsync_WithStagingCredentials_ReturnsManagedKey` |
