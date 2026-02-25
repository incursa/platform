namespace Incursa.Platform.Audit.WorkOS;

using Incursa.Platform.Audit;

public sealed class WorkOsAuditOutboxSerializer : IAuditOutboxSinkSerializer
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly WorkOsAuditSinkOptions options;

    public WorkOsAuditOutboxSerializer(WorkOsAuditSinkOptions options)
    {
        this.options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public string SinkName => "workos";

    public ValueTask<AuditOutboxSinkMessage?> SerializeAsync(AuditEvent auditEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(auditEvent);

        options.ActionVersions.TryGetValue(auditEvent.Name, out var mappedVersion);
        var version = mappedVersion > 0 ? mappedVersion : (int?)null;

        var envelope = new WorkOsAuditOutboxEnvelope(
            EventId: auditEvent.EventId.Value,
            OccurredAtUtc: auditEvent.OccurredAtUtc,
            Action: auditEvent.Name,
            DisplayMessage: auditEvent.DisplayMessage,
            Outcome: auditEvent.Outcome.ToString(),
            DataJson: auditEvent.DataJson,
            ActorType: auditEvent.Actor?.ActorType,
            ActorId: auditEvent.Actor?.ActorId,
            ActorDisplay: auditEvent.Actor?.ActorDisplay,
            Version: version,
            Anchors: auditEvent.Anchors.Select(static anchor => new WorkOsAuditOutboxAnchor(anchor.AnchorType, anchor.AnchorId, anchor.Role)).ToArray(),
            Correlation: auditEvent.Correlation is null
                ? null
                : new WorkOsAuditOutboxCorrelation(
                    auditEvent.Correlation.CorrelationId.Value,
                    auditEvent.Correlation.CausationId?.Value,
                    auditEvent.Correlation.TraceId,
                    auditEvent.Correlation.SpanId,
                    auditEvent.Correlation.Tags));

        var payload = JsonSerializer.Serialize(envelope, SerializerOptions);
        return ValueTask.FromResult<AuditOutboxSinkMessage?>(new AuditOutboxSinkMessage(
            options.OutboxTopic,
            payload,
            auditEvent.Correlation?.CorrelationId.Value));
    }
}
