namespace Incursa.Integrations.WorkOS.Abstractions.Audit;

public interface IWorkOsAuditClient
{
    ValueTask<WorkOsAuditCreateEventResult> CreateEventAsync(WorkOsAuditCreateEventRequest request, CancellationToken ct = default);
}
