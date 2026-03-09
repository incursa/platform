using Incursa.Integrations.Cloudflare.Storage;

namespace Incursa.Integrations.Cloudflare.Tests;

public sealed class InMemoryCloudflareKvStoreTests
{
    [Fact]
    public async Task PutGetDelete_WorksAsExpected()
    {
        InMemoryCloudflareKvStore sut = new();

        await sut.PutAsync("org:123", "active", CancellationToken.None);
        var value = await sut.GetAsync("org:123", CancellationToken.None);
        await sut.DeleteAsync("org:123", CancellationToken.None);
        var deleted = await sut.GetAsync("org:123", CancellationToken.None);

        Assert.Equal("active", value);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task ListKeysAsync_FiltersByPrefix_AndReturnsLexicographicOrder()
    {
        InMemoryCloudflareKvStore sut = new();
        await sut.PutAsync("pref:c", "3", CancellationToken.None);
        await sut.PutAsync("pref:a", "1", CancellationToken.None);
        await sut.PutAsync("pref:b", "2", CancellationToken.None);
        await sut.PutAsync("other:z", "9", CancellationToken.None);

        List<string> keys = new();
        await foreach (var key in sut.ListKeysAsync("pref:", CancellationToken.None))
        {
            keys.Add(key);
        }

        Assert.Equal(new[] { "pref:a", "pref:b", "pref:c" }, keys);
    }

    [Fact]
    public async Task DeleteAsync_MissingKey_IsNoOp()
    {
        InMemoryCloudflareKvStore sut = new();
        await sut.DeleteAsync("missing", CancellationToken.None);
        var value = await sut.GetAsync("missing", CancellationToken.None);
        Assert.Null(value);
    }

    [Fact]
    public async Task ListKeysAsync_NullPrefix_ReturnsAllKeysSorted()
    {
        InMemoryCloudflareKvStore sut = new();
        await sut.PutAsync("c", "3", CancellationToken.None);
        await sut.PutAsync("a", "1", CancellationToken.None);
        await sut.PutAsync("b", "2", CancellationToken.None);

        List<string> keys = new();
        await foreach (var key in sut.ListKeysAsync(null!, CancellationToken.None))
        {
            keys.Add(key);
        }

        Assert.Equal(new[] { "a", "b", "c" }, keys);
    }

    [Fact]
    public async Task ListKeysAsync_UsesSnapshot_WhenCollectionMutatesDuringEnumeration()
    {
        InMemoryCloudflareKvStore sut = new();
        await sut.PutAsync("pref:a", "1", CancellationToken.None);

        List<string> keys = new();
        await foreach (var key in sut.ListKeysAsync("pref:", CancellationToken.None))
        {
            keys.Add(key);
            await sut.PutAsync("pref:z", "99", CancellationToken.None);
        }

        Assert.Equal(new[] { "pref:a" }, keys);
    }

    [Fact]
    public async Task Methods_RespectCancellation()
    {
        InMemoryCloudflareKvStore sut = new();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => sut.PutAsync("k", "v", cts.Token).AsTask());
        await Assert.ThrowsAsync<OperationCanceledException>(() => sut.GetAsync("k", cts.Token).AsTask());
        await Assert.ThrowsAsync<OperationCanceledException>(() => sut.DeleteAsync("k", cts.Token).AsTask());
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await foreach (var ignored in sut.ListKeysAsync("k", cts.Token))
            {
            }
        });
    }

    [Fact]
    public async Task ConcurrentPutAndGet_RemainsConsistent()
    {
        InMemoryCloudflareKvStore sut = new();
        var keys = Enumerable.Range(0, 150).Select(static i => $"k{i:000}").ToArray();

        await Parallel.ForEachAsync(keys, async (key, ct) =>
        {
            await sut.PutAsync(key, $"v:{key}", ct);
            var value = await sut.GetAsync(key, ct);
            Assert.Equal($"v:{key}", value);
        });

        List<string> listed = new();
        await foreach (var key in sut.ListKeysAsync("k", CancellationToken.None))
        {
            listed.Add(key);
        }

        Assert.Equal(keys.Length, listed.Count);
    }
}
