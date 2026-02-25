namespace Incursa.Platform.Audit.WorkOS;

using Incursa.Platform.Audit;
using Incursa.Platform.Audit.WorkOS.Internal;
using Microsoft.Extensions.Logging;

public sealed class WorkOsAuditOutboxHandler : IOutboxHandler
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly WorkOsAuditSinkOptions options;
    private readonly IWorkOsAuditOrganizationResolver organizationResolver;
    private readonly IWorkOsAuditPublisher publisher;
    private readonly ILogger<WorkOsAuditOutboxHandler> logger;

    public WorkOsAuditOutboxHandler(
        WorkOsAuditSinkOptions options,
        IWorkOsAuditOrganizationResolver organizationResolver,
        IWorkOsAuditPublisher publisher,
        ILogger<WorkOsAuditOutboxHandler> logger)
    {
        this.options = options ?? throw new ArgumentNullException(nameof(options));
        this.organizationResolver = organizationResolver ?? throw new ArgumentNullException(nameof(organizationResolver));
        this.publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string Topic => options.OutboxTopic;

    public async Task HandleAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        WorkOsAuditOutboxEnvelope envelope;
        try
        {
            envelope = JsonSerializer.Deserialize<WorkOsAuditOutboxEnvelope>(message.Payload, SerializerOptions)
                ?? throw new OutboxPermanentFailureException("WorkOS audit sink payload is empty.");
        }
        catch (JsonException ex)
        {
            throw new OutboxPermanentFailureException("WorkOS audit sink payload is invalid JSON.", ex);
        }

        var organizationId = await ResolveOrganizationIdAsync(envelope, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(organizationId))
        {
            throw new OutboxPermanentFailureException($"Unable to resolve WorkOS organization id for audit event '{envelope.EventId}'.");
        }

        try
        {
            await publisher.PublishAsync(organizationId, envelope, options, cancellationToken).ConfigureAwait(false);
        }
        catch (WorkOsAuditPublishException ex) when (ex.FailureKind == WorkOsAuditPublishFailureKind.Permanent)
        {
            logger.LogWarning(
                ex,
                "Permanent failure publishing WorkOS audit event '{EventId}'.",
                envelope.EventId);
            throw new OutboxPermanentFailureException(ex.Message, ex);
        }
    }

    private async ValueTask<string?> ResolveOrganizationIdAsync(WorkOsAuditOutboxEnvelope envelope, CancellationToken cancellationToken)
    {
        var orgAnchor = envelope.Anchors.FirstOrDefault(anchor => options.OrganizationAnchorTypes.Contains(anchor.AnchorType));
        if (orgAnchor is not null)
        {
            return orgAnchor.AnchorId;
        }

        var tenantAnchor = envelope.Anchors.FirstOrDefault(anchor => options.TenantAnchorTypes.Contains(anchor.AnchorType));
        if (tenantAnchor is null)
        {
            return null;
        }

        var auditEvent = new AuditEvent(
            new AuditEventId(envelope.EventId),
            envelope.OccurredAtUtc,
            envelope.Action,
            envelope.DisplayMessage,
            Enum.TryParse<EventOutcome>(envelope.Outcome, true, out var parsedOutcome) ? parsedOutcome : EventOutcome.Info,
            envelope.Anchors.Select(static anchor => new EventAnchor(anchor.AnchorType, anchor.AnchorId, anchor.Role)).ToArray(),
            envelope.DataJson,
            envelope.ActorId is null || envelope.ActorType is null
                ? null
                : new AuditActor(envelope.ActorType, envelope.ActorId, envelope.ActorDisplay),
            correlation: null);

        return await organizationResolver.ResolveOrganizationIdAsync(tenantAnchor.AnchorId, auditEvent, cancellationToken).ConfigureAwait(false);
    }
}
