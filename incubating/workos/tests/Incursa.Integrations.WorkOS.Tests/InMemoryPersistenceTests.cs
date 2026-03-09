namespace Incursa.Integrations.WorkOS.Tests;

using Incursa.Integrations.WorkOS.Persistence.InMemory;

[TestClass]
public sealed class InMemoryPersistenceTests
{
    [TestMethod]
    public async Task DedupStore_ReturnsFalseOnSecondAcquire()
    {
        var store = new InMemoryWorkOsWebhookEventDedupStore();
        var first = await store.TryAcquireAsync("evt_1", DateTimeOffset.UtcNow, TimeSpan.FromHours(1));
        var second = await store.TryAcquireAsync("evt_1", DateTimeOffset.UtcNow, TimeSpan.FromHours(1));

        Assert.IsTrue(first);
        Assert.IsFalse(second);
    }
}

