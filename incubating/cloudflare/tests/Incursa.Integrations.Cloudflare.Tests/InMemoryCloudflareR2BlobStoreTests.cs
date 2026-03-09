using System.Text;
using Incursa.Integrations.Cloudflare.Storage;

namespace Incursa.Integrations.Cloudflare.Tests;

public sealed class InMemoryCloudflareR2BlobStoreTests
{
    [Fact]
    public async Task PutGetHeadDelete_WorksAsExpected()
    {
        InMemoryCloudflareR2BlobStore sut = new();
        var input = new MemoryStream(Encoding.UTF8.GetBytes("hello-world"));

        await sut.PutAsync("a/key.txt", input, "text/plain", CancellationToken.None);

        await using var stream = await sut.GetAsync("a/key.txt", CancellationToken.None);
        Assert.NotNull(stream);
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: false);
        var text = await reader.ReadToEndAsync(CancellationToken.None);
        var metadata = await sut.HeadAsync("a/key.txt", CancellationToken.None);

        Assert.Equal("hello-world", text);
        Assert.NotNull(metadata);
        Assert.Equal("a/key.txt", metadata.Key);
        Assert.Equal(11L, metadata.Size);
        Assert.Equal("text/plain", metadata.ContentType);
        Assert.True(metadata.LastModifiedUtc.HasValue);

        await sut.DeleteAsync("a/key.txt", CancellationToken.None);
        var missing = await sut.GetAsync("a/key.txt", CancellationToken.None);
        Assert.Null(missing);
    }

    [Fact]
    public async Task GetAsync_ReturnsIndependentStreamCopy()
    {
        InMemoryCloudflareR2BlobStore sut = new();
        await sut.PutAsync("k", new MemoryStream(Encoding.UTF8.GetBytes("abcdef")), null, CancellationToken.None);

        await using var first = await sut.GetAsync("k", CancellationToken.None);
        Assert.NotNull(first);
        _ = first.ReadByte();
        _ = first.ReadByte();

        await using var second = await sut.GetAsync("k", CancellationToken.None);
        Assert.NotNull(second);
        Assert.Equal(0L, second.Position);
    }

    [Fact]
    public async Task GetRangeAsync_HandlesValidAndInvalidRanges()
    {
        InMemoryCloudflareR2BlobStore sut = new();
        await sut.PutAsync("k", new MemoryStream(Encoding.UTF8.GetBytes("abcdef")), null, CancellationToken.None);

        await using var range = await sut.GetRangeAsync("k", 1, 3, CancellationToken.None);
        Assert.NotNull(range);
        using var reader = new StreamReader(range, Encoding.UTF8, leaveOpen: false);
        var rangeText = await reader.ReadToEndAsync(CancellationToken.None);

        var invalid1 = await sut.GetRangeAsync("k", -1, 2, CancellationToken.None);
        var invalid2 = await sut.GetRangeAsync("k", 4, 3, CancellationToken.None);
        var invalid3 = await sut.GetRangeAsync("k", 99, 199, CancellationToken.None);
        var clamped = await sut.GetRangeAsync("k", 4, 999, CancellationToken.None);
        using var clampedReader = new StreamReader(clamped!, Encoding.UTF8, leaveOpen: false);
        var clampedText = await clampedReader.ReadToEndAsync(CancellationToken.None);

        Assert.Equal("bcd", rangeText);
        Assert.Null(invalid1);
        Assert.Null(invalid2);
        Assert.Null(invalid3);
        Assert.Equal("ef", clampedText);
    }

    [Fact]
    public async Task ListAsync_FiltersByPrefix_AndReturnsLexicographicOrder()
    {
        InMemoryCloudflareR2BlobStore sut = new();
        await sut.PutAsync("pref/c", new MemoryStream([3]), null, CancellationToken.None);
        await sut.PutAsync("pref/a", new MemoryStream([1]), null, CancellationToken.None);
        await sut.PutAsync("pref/b", new MemoryStream([2]), null, CancellationToken.None);
        await sut.PutAsync("other/z", new MemoryStream([9]), null, CancellationToken.None);

        List<string> keys = new();
        await foreach (var item in sut.ListAsync("pref/", CancellationToken.None))
        {
            keys.Add(item.Key);
        }

        Assert.Equal(new[] { "pref/a", "pref/b", "pref/c" }, keys);
    }

    [Fact]
    public async Task MissingObjects_ReturnNull_AndDeleteIsNoOp()
    {
        InMemoryCloudflareR2BlobStore sut = new();
        var get = await sut.GetAsync("missing", CancellationToken.None);
        var range = await sut.GetRangeAsync("missing", 0, 5, CancellationToken.None);
        var head = await sut.HeadAsync("missing", CancellationToken.None);
        await sut.DeleteAsync("missing", CancellationToken.None);

        Assert.Null(get);
        Assert.Null(range);
        Assert.Null(head);
    }

    [Fact]
    public async Task PutAsync_NullContent_Throws()
    {
        InMemoryCloudflareR2BlobStore sut = new();
        await Assert.ThrowsAsync<ArgumentNullException>(() => sut.PutAsync("k", null!, null, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task ListAsync_NullPrefix_ReturnsAllKeysSorted()
    {
        InMemoryCloudflareR2BlobStore sut = new();
        await sut.PutAsync("c", new MemoryStream([3]), null, CancellationToken.None);
        await sut.PutAsync("a", new MemoryStream([1]), null, CancellationToken.None);
        await sut.PutAsync("b", new MemoryStream([2]), null, CancellationToken.None);

        List<string> keys = new();
        await foreach (var item in sut.ListAsync(null!, CancellationToken.None))
        {
            keys.Add(item.Key);
        }

        Assert.Equal(new[] { "a", "b", "c" }, keys);
    }

    [Fact]
    public async Task ListAsync_UsesSnapshot_WhenCollectionMutatesDuringEnumeration()
    {
        InMemoryCloudflareR2BlobStore sut = new();
        await sut.PutAsync("pref/a", new MemoryStream([1]), null, CancellationToken.None);

        List<string> keys = new();
        await foreach (var item in sut.ListAsync("pref/", CancellationToken.None))
        {
            keys.Add(item.Key);
            await sut.PutAsync("pref/z", new MemoryStream([9]), null, CancellationToken.None);
        }

        Assert.Equal(new[] { "pref/a" }, keys);
    }

    [Fact]
    public async Task Methods_RespectCancellation()
    {
        InMemoryCloudflareR2BlobStore sut = new();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            sut.PutAsync("k", new MemoryStream([1]), null, cts.Token).AsTask());
        await Assert.ThrowsAsync<OperationCanceledException>(() => sut.GetAsync("k", cts.Token).AsTask());
        await Assert.ThrowsAsync<OperationCanceledException>(() => sut.GetRangeAsync("k", 0, 1, cts.Token).AsTask());
        await Assert.ThrowsAsync<OperationCanceledException>(() => sut.HeadAsync("k", cts.Token).AsTask());
        await Assert.ThrowsAsync<OperationCanceledException>(() => sut.DeleteAsync("k", cts.Token).AsTask());
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await foreach (var ignored in sut.ListAsync("k", cts.Token))
            {
            }
        });
    }

    [Fact]
    public async Task ConcurrentPutAndGet_RemainsConsistent()
    {
        InMemoryCloudflareR2BlobStore sut = new();
        var keys = Enumerable.Range(0, 120).Select(static i => $"obj/{i:000}").ToArray();

        await Parallel.ForEachAsync(keys, async (key, ct) =>
        {
            await sut.PutAsync(key, new MemoryStream(Encoding.UTF8.GetBytes(key)), "text/plain", ct);
            await using var stream = await sut.GetAsync(key, ct);
            Assert.NotNull(stream);
            using StreamReader reader = new(stream, Encoding.UTF8, leaveOpen: false);
            var payload = await reader.ReadToEndAsync(ct);
            Assert.Equal(key, payload);
        });

        List<string> listed = new();
        await foreach (var item in sut.ListAsync("obj/", CancellationToken.None))
        {
            listed.Add(item.Key);
        }

        Assert.Equal(keys.Length, listed.Count);
    }
}
