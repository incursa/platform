namespace Incursa.Integrations.WorkOS.Abstractions.Audit;

public sealed class WorkOsAuditClientException : Exception
{
    public WorkOsAuditClientException()
    {
    }

    public WorkOsAuditClientException(string? message)
        : base(message)
    {
    }

    public WorkOsAuditClientException(string message, WorkOsAuditFailureKind failureKind, int? statusCode = null, Exception? innerException = null)
        : base(message, innerException)
    {
        FailureKind = failureKind;
        StatusCode = statusCode;
    }

    public WorkOsAuditClientException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }

    public WorkOsAuditFailureKind FailureKind { get; }

    public int? StatusCode { get; }
}
