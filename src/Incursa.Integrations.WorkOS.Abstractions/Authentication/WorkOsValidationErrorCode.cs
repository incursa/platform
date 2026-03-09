namespace Incursa.Integrations.WorkOS.Abstractions.Authentication;

public enum WorkOsValidationErrorCode
{
    None = 0,
    Invalid,
    Revoked,
    Expired,
    UnknownOrganization,
    InsufficientScope,
    InternalError,
}

