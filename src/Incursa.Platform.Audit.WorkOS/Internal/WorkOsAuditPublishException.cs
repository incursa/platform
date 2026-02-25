namespace Incursa.Platform.Audit.WorkOS.Internal;

internal sealed class WorkOsAuditPublishException : Exception
{
    public WorkOsAuditPublishException()
    {
    }

    public WorkOsAuditPublishException(string? message)
        : base(message)
    {
    }

    public WorkOsAuditPublishException(string message, WorkOsAuditPublishFailureKind failureKind, int? statusCode = null, Exception? innerException = null)
        : base(message, innerException)
    {
        FailureKind = failureKind;
        StatusCode = statusCode;
    }

    public WorkOsAuditPublishException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }

    public WorkOsAuditPublishFailureKind FailureKind { get; }

    public int? StatusCode { get; }
}
