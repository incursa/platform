using Incursa.Platform.Audit;
using Incursa.Platform.Audit.WorkOS;
using Incursa.Platform.Audit.WorkOS.Internal;
using Incursa.Platform.Outbox;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using System.Data;

namespace Incursa.Platform.Tests;

public sealed class WorkOsAuditSinkTests
{
    [Fact]
    public async Task WorkOsAuditOutboxSerializer_SerializesOutboxMessage()
    {
        var options = new WorkOsAuditSinkOptions();
        var serializer = new WorkOsAuditOutboxSerializer(options);
        var auditEvent = CreateAuditEvent();

        var message = await serializer.SerializeAsync(auditEvent, CancellationToken.None);

        Assert.NotNull(message);
        Assert.Equal(options.OutboxTopic, message!.Topic);
        Assert.Contains("user.signed_in", message.Payload, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AuditFanoutWriter_EnqueuesSinkPayload_AfterPrimaryWrite()
    {
        var serializer = new FakeSerializer();
        var outbox = new FakeOutbox();
        var primary = new FakeAuditEventWriter();
        var services = new ServiceCollection();
        services.AddSingleton<IAuditEventWriter>(primary);
        services.AddSingleton<IOutbox>(outbox);
        services.AddSingleton<IAuditOutboxSinkSerializer>(serializer);
        services.AddLogging();
        services.AddAuditSinkFanout();

        var provider = services.BuildServiceProvider();
        var writer = provider.GetRequiredService<IAuditEventWriter>();
        var auditEvent = CreateAuditEvent();

        await writer.WriteAsync(auditEvent, CancellationToken.None);

        Assert.Equal(1, primary.WriteCalls);
        Assert.Single(outbox.Items);
        Assert.Equal("audit.sink.test", outbox.Items[0].Topic);
    }

    [Fact]
    public async Task WorkOsAuditOutboxHandler_UnresolvedOrganization_ThrowsPermanentFailure()
    {
        var options = new WorkOsAuditSinkOptions();
        var serializer = new WorkOsAuditOutboxSerializer(options);
        var auditEvent = CreateAuditEvent();
        var sinkMessage = await serializer.SerializeAsync(auditEvent, CancellationToken.None);
        Assert.NotNull(sinkMessage);

        var handler = new WorkOsAuditOutboxHandler(
            options,
            NullWorkOsAuditOrganizationResolver.Instance,
            new FakePublisher(),
            NullLogger<WorkOsAuditOutboxHandler>.Instance);

        var outboxMessage = new OutboxMessage
        {
            Topic = options.OutboxTopic,
            Payload = sinkMessage!.Payload,
            MessageId = OutboxMessageIdentifier.GenerateNew(),
            Id = OutboxWorkItemIdentifier.GenerateNew(),
            CreatedAt = DateTimeOffset.UtcNow,
        };

        await Assert.ThrowsAsync<OutboxPermanentFailureException>(async () =>
            await handler.HandleAsync(outboxMessage, CancellationToken.None));
    }

    private static AuditEvent CreateAuditEvent()
    {
        return new AuditEvent(
            AuditEventId.NewId(),
            DateTimeOffset.UtcNow,
            "user.signed_in",
            "User signed in",
            EventOutcome.Success,
            [new EventAnchor("tenant", "tenant_1", "Subject"), new EventAnchor("resource", "resource_1", "Participant")],
            "{\"ip\":\"127.0.0.1\"}",
            new AuditActor("user", "user_1", "User One"),
            correlation: null);
    }

    private sealed class FakeAuditEventWriter : IAuditEventWriter
    {
        public int WriteCalls { get; private set; }

        public Task WriteAsync(AuditEvent auditEvent, CancellationToken cancellationToken)
        {
            WriteCalls++;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeSerializer : IAuditOutboxSinkSerializer
    {
        public string SinkName => "fake";

        public ValueTask<AuditOutboxSinkMessage?> SerializeAsync(AuditEvent auditEvent, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult<AuditOutboxSinkMessage?>(new AuditOutboxSinkMessage("audit.sink.test", "{\"ok\":true}"));
        }
    }

    private sealed class FakeOutbox : IOutbox
    {
        public List<(string Topic, string Payload, string? CorrelationId)> Items { get; } = [];

        public Task EnqueueAsync(string topic, string payload, CancellationToken cancellationToken)
            => EnqueueAsync(topic, payload, correlationId: null, cancellationToken);

        public Task EnqueueAsync(string topic, string payload, string? correlationId, CancellationToken cancellationToken)
        {
            Items.Add((topic, payload, correlationId));
            return Task.CompletedTask;
        }

        public Task EnqueueAsync(string topic, string payload, string? correlationId, DateTimeOffset? dueTimeUtc, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task EnqueueAsync(string topic, string payload, IDbTransaction transaction, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task EnqueueAsync(string topic, string payload, IDbTransaction transaction, string? correlationId, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task EnqueueAsync(string topic, string payload, IDbTransaction transaction, string? correlationId, DateTimeOffset? dueTimeUtc, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task<IReadOnlyList<OutboxWorkItemIdentifier>> ClaimAsync(OwnerToken ownerToken, int leaseSeconds, int batchSize, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task AckAsync(OwnerToken ownerToken, IEnumerable<OutboxWorkItemIdentifier> ids, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task AbandonAsync(OwnerToken ownerToken, IEnumerable<OutboxWorkItemIdentifier> ids, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task FailAsync(OwnerToken ownerToken, IEnumerable<OutboxWorkItemIdentifier> ids, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task ReapExpiredAsync(CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task<JoinIdentifier> StartJoinAsync(long tenantId, int expectedSteps, string? metadata, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task AttachMessageToJoinAsync(JoinIdentifier joinId, OutboxMessageIdentifier outboxMessageId, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task ReportStepCompletedAsync(JoinIdentifier joinId, OutboxMessageIdentifier outboxMessageId, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task ReportStepFailedAsync(JoinIdentifier joinId, OutboxMessageIdentifier outboxMessageId, CancellationToken cancellationToken)
            => throw new NotImplementedException();
    }

    private sealed class FakePublisher : IWorkOsAuditPublisher
    {
        public ValueTask PublishAsync(string organizationId, WorkOsAuditOutboxEnvelope envelope, WorkOsAuditSinkOptions options, CancellationToken cancellationToken)
            => ValueTask.CompletedTask;
    }
}
