using System.Net;
using Incursa.Integrations.Cloudflare.Abstractions;
using Incursa.Integrations.Cloudflare.Internal;
using Incursa.Integrations.Cloudflare.Options;
using Incursa.Integrations.Cloudflare.Tests.TestInfrastructure;
using Microsoft.Extensions.Logging.Abstractions;

namespace Incursa.Integrations.Cloudflare.Tests;

public sealed class CloudflareApiTransportRobustnessTests
{
    [Fact]
    public async Task SendForRawAsync_ThrowsTimeout_WhenBodyNeverCompletesAsync()
    {
        var sut = CreateTransport(
            new StubHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StreamContent(new NeverEndingReadStream()),
            })),
            requestTimeoutSeconds: 1);

        Stopwatch sw = Stopwatch.StartNew();
        var ex = await Assert.ThrowsAsync<CloudflareApiException>(() =>
            sut.SendForRawAsync(HttpMethod.Get, "zones/z1/load_balancers", body: null, CancellationToken.None));

        sw.Stop();
        Assert.Contains("timed out", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(6), $"Raw call did not fail fast. elapsed={sw.Elapsed}");
    }

    [Fact]
    public async Task SendForResultAsync_ThrowsTimeout_WhenBodyNeverCompletesAsync()
    {
        var sut = CreateTransport(
            new StubHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StreamContent(new NeverEndingReadStream()),
            })),
            requestTimeoutSeconds: 1);

        Stopwatch sw = Stopwatch.StartNew();
        var ex = await Assert.ThrowsAsync<CloudflareApiException>(() =>
            sut.SendForResultAsync<object>(HttpMethod.Get, "zones/z1/load_balancers", body: null, CancellationToken.None));

        sw.Stop();
        Assert.Contains("timed out", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(6), $"Result call did not fail fast. elapsed={sw.Elapsed}");
    }

    [Fact]
    public async Task SendForResultAsync_ThrowsCloudflareApiException_OnMalformedJsonAsync()
    {
        var sut = CreateTransport(new StubHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{not-json"),
        })));

        await Assert.ThrowsAsync<CloudflareApiException>(() => sut.SendForResultAsync<object>(HttpMethod.Get, "zones/z1/load_balancers", body: null, CancellationToken.None));
    }

    [Fact]
    public async Task SendForRawAsync_HonorsCancellationAsync()
    {
        var sut = CreateTransport(new StubHttpMessageHandler(async (_, cancellationToken) =>
        {
            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}"),
            };
        }));

        using CancellationTokenSource cts = new(TimeSpan.FromMilliseconds(50));
        await Assert.ThrowsAsync<TaskCanceledException>(() => sut.SendForRawAsync(HttpMethod.Get, "zones/z1/load_balancers", body: null, cts.Token));
    }

    [Fact]
    [Trait("Category", "Fuzz")]
    public async Task SendForResultAsync_DoesNotCrash_OnRandomInvalidPayloadsAsync()
    {
        Random random = new(42);
        for (var i = 0; i < 100; i++)
        {
            var payload = CreateNoisePayload(random, i);
            var sut = CreateTransport(new StubHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(payload),
            })));

            try
            {
                _ = await sut.SendForResultAsync<object>(HttpMethod.Get, "zones/z1/load_balancers", body: null, CancellationToken.None);
            }
            catch (CloudflareApiException)
            {
            }
        }
    }

    private static CloudflareApiTransport CreateTransport(HttpMessageHandler handler, int requestTimeoutSeconds = 8)
    {
        HttpClient httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.cloudflare.com/client/v4/"),
        };

        return new CloudflareApiTransport(
            httpClient,
            Microsoft.Extensions.Options.Options.Create(new CloudflareApiOptions
            {
                ApiToken = "token-123",
                RetryCount = 0,
                RequestTimeoutSeconds = requestTimeoutSeconds,
            }),
            NullLogger<CloudflareApiTransport>.Instance);
    }

    private static string CreateNoisePayload(Random random, int i)
    {
        const string Alphabet = "abcdefghijklmnopqrstuvwxyz{}[],:\"0123456789";
        var len = random.Next(1, 120);
        Span<char> buffer = len <= 256 ? stackalloc char[len] : new char[len];
        for (var idx = 0; idx < len; idx++)
        {
            buffer[idx] = Alphabet[random.Next(0, Alphabet.Length)];
        }

        return i % 7 == 0 ? string.Empty : new string(buffer);
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
}
