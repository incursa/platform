namespace Incursa.Platform.Audit.WorkOS;

using Incursa.Platform.Audit;
using Incursa.Platform.Audit.WorkOS.Internal;
using Microsoft.Extensions.Logging;

internal sealed class AuditFanoutWriter : IAuditEventWriter
{
    private readonly IPrimaryAuditEventWriter primaryWriter;
    private readonly IReadOnlyList<IAuditOutboxSinkSerializer> serializers;
    private readonly IOutbox outbox;
    private readonly ILogger<AuditFanoutWriter> logger;

    public AuditFanoutWriter(
        IPrimaryAuditEventWriter primaryWriter,
        IEnumerable<IAuditOutboxSinkSerializer> serializers,
        IOutbox outbox,
        ILogger<AuditFanoutWriter> logger)
    {
        this.primaryWriter = primaryWriter ?? throw new ArgumentNullException(nameof(primaryWriter));
        this.serializers = (serializers ?? throw new ArgumentNullException(nameof(serializers))).ToArray();
        this.outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task WriteAsync(AuditEvent auditEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(auditEvent);

        await primaryWriter.WriteAsync(auditEvent, cancellationToken).ConfigureAwait(false);

        if (serializers.Count == 0)
        {
            return;
        }

        var correlationId = auditEvent.Correlation?.CorrelationId.Value;

        foreach (var serializer in serializers)
        {
            try
            {
                var outboxMessage = await serializer.SerializeAsync(auditEvent, cancellationToken).ConfigureAwait(false);
                if (outboxMessage is null)
                {
                    continue;
                }

                await outbox.EnqueueAsync(
                    outboxMessage.Topic,
                    outboxMessage.Payload,
                    string.IsNullOrWhiteSpace(outboxMessage.CorrelationId) ? correlationId : outboxMessage.CorrelationId,
                    cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    ex,
                    "Failed to enqueue audit sink message for sink '{SinkName}' and event '{AuditEventId}'.",
                    serializer.SinkName,
                    auditEvent.EventId.Value);
            }
        }
    }
}
