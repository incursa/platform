namespace Incursa.Integrations.WorkOS.Tests;

using Incursa.Integrations.WorkOS.Abstractions.Persistence;
using Incursa.Integrations.WorkOS.Abstractions.Webhooks;
using Incursa.Integrations.WorkOS.Core.Webhooks;

[TestClass]
public sealed class WorkOsWebhookProcessorTests
{
    [TestMethod]
    public async Task ProcessAsync_FirstDelivery_InvokesHandlersAndReturnsProcessed()
    {
        var dedupe = new FakeDedupeStore(acquireResult: true);
        var handler = new RecordingHandler();
        var sut = new WorkOsWebhookProcessor(dedupe, [handler], TimeSpan.FromHours(1));

        var result = await sut.ProcessAsync(CreateEvent()).ConfigureAwait(false);

        Assert.IsTrue(result.Processed);
        Assert.IsFalse(result.Duplicate);
        Assert.AreEqual(1, handler.CallCount);
    }

    [TestMethod]
    public async Task ProcessAsync_DuplicateDelivery_ReturnsDuplicateWithoutInvokingHandlers()
    {
        var dedupe = new FakeDedupeStore(acquireResult: false);
        var handler = new RecordingHandler();
        var sut = new WorkOsWebhookProcessor(dedupe, [handler], TimeSpan.FromHours(1));

        var result = await sut.ProcessAsync(CreateEvent()).ConfigureAwait(false);

        Assert.IsFalse(result.Processed);
        Assert.IsTrue(result.Duplicate);
        Assert.AreEqual("duplicate_event", result.Message);
        Assert.AreEqual(0, handler.CallCount);
    }

    [TestMethod]
    public async Task ProcessAsync_HandlerThrows_BubblesException()
    {
        var dedupe = new FakeDedupeStore(acquireResult: true);
        var sut = new WorkOsWebhookProcessor(dedupe, [new ThrowingHandler()], TimeSpan.FromHours(1));

        try
        {
            _ = await sut.ProcessAsync(CreateEvent()).ConfigureAwait(false);
        }
        catch (InvalidOperationException)
        {
            return;
        }

        Assert.Fail("Expected InvalidOperationException.");
    }

    private static WorkOsWebhookEvent CreateEvent()
    {
        var payload = JsonDocument.Parse("{\"id\":\"evt_1\",\"event\":\"organization_membership.updated\"}");
        return new WorkOsWebhookEvent("evt_1", "organization_membership.updated", "org_1", DateTimeOffset.UtcNow, payload);
    }

    private sealed class FakeDedupeStore : IWorkOsWebhookEventDedupStore
    {
        private readonly bool _acquireResult;

        public FakeDedupeStore(bool acquireResult)
        {
            _acquireResult = acquireResult;
        }

        public ValueTask<bool> TryAcquireAsync(string eventId, DateTimeOffset seenUtc, TimeSpan ttl, CancellationToken ct = default)
            => ValueTask.FromResult(_acquireResult);
    }

    private sealed class RecordingHandler : IWorkOsWebhookEventHandler
    {
        public int CallCount { get; private set; }

        public ValueTask HandleAsync(WorkOsWebhookEvent webhookEvent, CancellationToken ct = default)
        {
            CallCount++;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class ThrowingHandler : IWorkOsWebhookEventHandler
    {
        public ValueTask HandleAsync(WorkOsWebhookEvent webhookEvent, CancellationToken ct = default)
            => ValueTask.FromException(new InvalidOperationException("boom"));
    }
}
