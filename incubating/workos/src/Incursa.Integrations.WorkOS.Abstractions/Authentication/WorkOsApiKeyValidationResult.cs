namespace Incursa.Integrations.WorkOS.Abstractions.Authentication;

public sealed record WorkOsApiKeyValidationResult(
    bool IsValid,
    WorkOsValidationErrorCode ErrorCode,
    string ErrorReason,
    WorkOsAuthIdentity? Identity)
{
    public static WorkOsApiKeyValidationResult Success(WorkOsAuthIdentity identity)
        => new WorkOsApiKeyValidationResult(true, WorkOsValidationErrorCode.None, string.Empty, identity);

    public static WorkOsApiKeyValidationResult Failure(WorkOsValidationErrorCode code, string reason)
        => new WorkOsApiKeyValidationResult(false, code, reason, null);
}

