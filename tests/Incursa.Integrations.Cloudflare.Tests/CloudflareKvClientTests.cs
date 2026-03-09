using System.Net;
using System.Text;
using Incursa.Integrations.Cloudflare.Abstractions;
using Incursa.Integrations.Cloudflare.Clients;
using Incursa.Integrations.Cloudflare.Internal;
using Incursa.Integrations.Cloudflare.Options;
using Incursa.Integrations.Cloudflare.Tests.TestInfrastructure;
using Microsoft.Extensions.Logging.Abstractions;

namespace Incursa.Integrations.Cloudflare.Tests;

public sealed class CloudflareKvClientTests
{
    [Fact]
    public async Task GetAsync_ReturnsNull_OnNotFoundAsync()
    {
        var sut = CreateClient(new StubHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent("not found"),
        })));

        var result = await sut.GetAsync("key-1", CancellationToken.None);
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAsync_Throws_OnUnexpectedStatusAsync()
    {
        var sut = CreateClient(new StubHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadGateway)
        {
            Content = new StringContent("upstream failed"),
        })));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.GetAsync("key-1", CancellationToken.None));
        Assert.Contains("Cloudflare KV GET failed", ex.Message);
    }

    [Fact]
    public async Task GetAsync_ThrowsTimeout_WhenBodyNeverCompletesAsync()
    {
        var sut = CreateClient(
            new StubHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StreamContent(new NeverEndingReadStream()),
            })),
            retryCount: 0,
            requestTimeoutSeconds: 1);

        Stopwatch sw = Stopwatch.StartNew();
        var ex = await Assert.ThrowsAsync<CloudflareApiException>(() => sut.GetAsync("key-1", CancellationToken.None));

        sw.Stop();
        Assert.Contains("timed out", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(6), $"GET did not fail fast. elapsed={sw.Elapsed}");
    }

    [Fact]
    public async Task PutAsync_ThrowsTimeout_WhenBodyNeverCompletesAsync()
    {
        var sut = CreateClient(
            new StubHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StreamContent(new NeverEndingReadStream()),
            })),
            retryCount: 0,
            requestTimeoutSeconds: 1);

        Stopwatch sw = Stopwatch.StartNew();
        var ex = await Assert.ThrowsAsync<CloudflareApiException>(() => sut.PutAsync("key-1", "value-1", CancellationToken.None));

        sw.Stop();
        Assert.Contains("timed out", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(6), $"PUT did not fail fast. elapsed={sw.Elapsed}");
    }

    [Fact]
    public async Task ListKeysAsync_Throws_OnRepeatedCursorAsync()
    {
        var callCount = 0;
        var sut = CreateClient(new StubHttpMessageHandler((_, _) =>
        {
            callCount++;
            var payload = "{\"success\":true,\"result\":[{\"name\":\"k1\"}],\"result_info\":{\"cursor\":\"same\"}}";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(payload),
            });
        }));

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (var item in sut.ListKeysAsync("pref", CancellationToken.None))
            {
                Assert.False(string.IsNullOrWhiteSpace(item));
            }
        });

        Assert.Equal(2, callCount);
    }

    [Fact]
    public async Task ListKeysAsync_ReturnsQuickly_WhenResultIsEmptyAsync()
    {
        var callCount = 0;
        var sut = CreateClient(new StubHttpMessageHandler((_, _) =>
        {
            callCount++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"success\":true,\"result\":[],\"result_info\":{}}"),
            });
        }));

        List<string> keys = new();
        await foreach (var key in sut.ListKeysAsync("consumer:", CancellationToken.None))
        {
            keys.Add(key);
        }

        Assert.Empty(keys);
        Assert.Equal(1, callCount);
    }

    [Fact]
    public async Task ListKeysAsync_ThrowsTimeout_WhenBodyNeverCompletesAsync()
    {
        var sut = CreateClient(
            new StubHttpMessageHandler((_, _) =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StreamContent(new NeverEndingReadStream()),
                })),
            retryCount: 0,
            requestTimeoutSeconds: 30,
            listOperationTimeoutSeconds: 1);

        Stopwatch sw = Stopwatch.StartNew();
        var ex = await Assert.ThrowsAsync<TimeoutException>(async () =>
        {
            await foreach (var ignored in sut.ListKeysAsync("consumer:", CancellationToken.None))
            {
            }
        });

        sw.Stop();
        Assert.Contains("timed out", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(6), $"Listing did not fail fast. elapsed={sw.Elapsed}");
    }

    [Fact]
    public async Task ListKeysAsync_Throws_WhenResponseBodyIsInterruptedAsync()
    {
        var partialPayload = "{\"success\":true,\"result\":[{\"name\":\"consumer:1\"}],\"result_info\":{\"cursor\":\"next\"}}";
        var stream = new FailingReadStream(Encoding.UTF8.GetBytes(partialPayload), failAfterBytes: 12);
        var sut = CreateClient(
            new StubHttpMessageHandler((_, _) =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StreamContent(stream),
                })),
            retryCount: 0,
            requestTimeoutSeconds: 30,
            listOperationTimeoutSeconds: 5);

        var ex = await Assert.ThrowsAsync<CloudflareApiException>(async () =>
        {
            await foreach (var ignored in sut.ListKeysAsync("consumer:", CancellationToken.None))
            {
            }
        });

        Assert.Contains("request failed", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static CloudflareKvClient CreateClient(
        HttpMessageHandler handler,
        int retryCount = 0,
        int requestTimeoutSeconds = 8,
        int listOperationTimeoutSeconds = 5)
    {
        HttpClient httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.cloudflare.com/client/v4/"),
        };

        var transport = new CloudflareApiTransport(
            httpClient,
            Microsoft.Extensions.Options.Options.Create(new CloudflareApiOptions
            {
                ApiToken = "token-123",
                RetryCount = retryCount,
                RequestTimeoutSeconds = requestTimeoutSeconds,
            }),
            NullLogger<CloudflareApiTransport>.Instance);

        return new CloudflareKvClient(
            transport,
            Microsoft.Extensions.Options.Options.Create(new CloudflareApiOptions
            {
                AccountId = "acct-1",
            }),
            Microsoft.Extensions.Options.Options.Create(new CloudflareKvOptions
            {
                NamespaceId = "ns-1",
                ListOperationTimeoutSeconds = listOperationTimeoutSeconds,
            }));
    }

    private sealed class NeverEndingReadStream : Stream
    {
        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
            => throw new NotSupportedException();

        public override int Read(byte[] buffer, int offset, int count)
            => throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin)
            => throw new NotSupportedException();

        public override void SetLength(long value)
            => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count)
            => throw new NotSupportedException();

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            return new ValueTask<int>(Task.Delay(Timeout.Infinite, cancellationToken).ContinueWith(
                static _ => 0,
                cancellationToken,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default));
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
    }

    private sealed class FailingReadStream : Stream
    {
        private readonly byte[] payload;
        private readonly int failAfterBytes;
        private int position;

        public FailingReadStream(byte[] payload, int failAfterBytes)
        {
            this.payload = payload ?? throw new ArgumentNullException(nameof(payload));
            this.failAfterBytes = failAfterBytes;
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => payload.Length;

        public override long Position
        {
            get => position;
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
            => ReadAsync(buffer.AsMemory(offset, count), CancellationToken.None).GetAwaiter().GetResult();

        public override long Seek(long offset, SeekOrigin origin)
            => throw new NotSupportedException();

        public override void SetLength(long value)
            => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count)
            => throw new NotSupportedException();

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (position >= failAfterBytes)
            {
                throw new IOException("Simulated interrupted chunked transfer.");
            }

            if (position >= payload.Length)
            {
                return new ValueTask<int>(0);
            }

            var remainingUntilFailure = failAfterBytes - position;
            var bytesToCopy = Math.Min(buffer.Length, Math.Min(payload.Length - position, remainingUntilFailure));
            payload.AsSpan(position, bytesToCopy).CopyTo(buffer.Span);
            position += bytesToCopy;
            return new ValueTask<int>(bytesToCopy);
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
    }
}
