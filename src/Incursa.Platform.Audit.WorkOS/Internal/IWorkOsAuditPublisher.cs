namespace Incursa.Platform.Audit.WorkOS.Internal;

public interface IWorkOsAuditPublisher
{
    ValueTask PublishAsync(string organizationId, WorkOsAuditOutboxEnvelope envelope, WorkOsAuditSinkOptions options, CancellationToken cancellationToken);
}
