namespace Incursa.Integrations.ElectronicNotary.Tests;

using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using Incursa.Integrations.ElectronicNotary.Proof.AspNetCore;
using Incursa.Platform;
using Incursa.Platform.Webhooks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

#pragma warning disable SA1011
[TestClass]
public sealed class ProofWebhookEndpointTests
{
    [TestMethod]
    public async Task InvalidSignatureIsRejectedAsync()
    {
        var inbox = new RecordingInbox();
        ServiceProvider serviceProvider = CreateServiceProvider(inbox, "proof-signing-key");
        IWebhookIngestor ingestor = serviceProvider.GetRequiredService<IWebhookIngestor>();
        var context = CreateContext("""{"event":"transaction.updated","transaction_id":"ot_123","date_occurred":"2026-02-06T00:00:00Z"}""");
        context.RequestServices = serviceProvider;
        context.Request.Headers[ProofWebhookOptions.SignatureHeaderName] = "invalid";

        IResult result = await ProofWebhookEndpoint.HandleAsync(context, ingestor, CancellationToken.None).ConfigureAwait(false);
        await result.ExecuteAsync(context).ConfigureAwait(false);

        context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        inbox.Enqueued.Count.Should().Be(0);
    }

    [TestMethod]
    public async Task ValidWebhookReturns200AndIsEnqueuedAsync()
    {
        var inbox = new RecordingInbox();
        const string SigningKey = "proof-signing-key";
        ServiceProvider serviceProvider = CreateServiceProvider(inbox, SigningKey);
        IWebhookIngestor ingestor = serviceProvider.GetRequiredService<IWebhookIngestor>();
        string body = """{"event":"transaction.updated","transaction_id":"ot_123","date_occurred":"2026-02-06T00:00:00Z"}""";
        var context = CreateContext(body);
        context.RequestServices = serviceProvider;
        context.Request.Headers[ProofWebhookOptions.SignatureHeaderName] = CreateSignature(body, SigningKey);

        IResult result = await ProofWebhookEndpoint.HandleAsync(context, ingestor, CancellationToken.None).ConfigureAwait(false);
        await result.ExecuteAsync(context).ConfigureAwait(false);

        context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
        inbox.Enqueued.Count.Should().Be(1);
    }

    private static ServiceProvider CreateServiceProvider(RecordingInbox inbox, string signingKey)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IInbox>(inbox);
        services.AddProofWebhooks(options =>
        {
            options.SigningKey = signingKey;
            options.RequireSignature = true;
        });

        return services.BuildServiceProvider();
    }

    private static DefaultHttpContext CreateContext(string body)
    {
        byte[] bodyBytes = Encoding.UTF8.GetBytes(body);
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/webhooks/proof";
        context.Request.ContentType = "application/json";
        context.Request.Body = new MemoryStream(bodyBytes);
        return context;
    }

    private static string CreateSignature(string body, string signingKey)
    {
        byte[] keyBytes = Encoding.UTF8.GetBytes(signingKey);
        using var hmac = new HMACSHA256(keyBytes);
        byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(body));
        return Convert.ToHexString(hash).ToUpperInvariant();
    }

    private sealed class RecordingInbox : IInbox
    {
        private readonly HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);

        public List<EnqueuedMessage> Enqueued { get; } = new List<EnqueuedMessage>();

        public Task<bool> AlreadyProcessedAsync(string messageId, string source, CancellationToken cancellationToken)
        {
            return Task.FromResult(!this.seen.Add(CreateKey(source, messageId)));
        }

        public Task<bool> AlreadyProcessedAsync(string messageId, string source, byte[]? hash, CancellationToken cancellationToken)
        {
            return Task.FromResult(!this.seen.Add(CreateKey(source, messageId)));
        }

        public Task MarkProcessedAsync(string messageId, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task MarkProcessingAsync(string messageId, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task MarkDeadAsync(string messageId, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task EnqueueAsync(string topic, string source, string messageId, string payload, CancellationToken cancellationToken)
        {
            this.Enqueued.Add(new EnqueuedMessage(topic, source, messageId, payload));
            return Task.CompletedTask;
        }

        public Task EnqueueAsync(string topic, string source, string messageId, string payload, byte[]? hash, CancellationToken cancellationToken)
        {
            this.Enqueued.Add(new EnqueuedMessage(topic, source, messageId, payload));
            return Task.CompletedTask;
        }

        public Task EnqueueAsync(string topic, string source, string messageId, string payload, byte[]? hash, DateTimeOffset? dueTimeUtc, CancellationToken cancellationToken)
        {
            this.Enqueued.Add(new EnqueuedMessage(topic, source, messageId, payload));
            return Task.CompletedTask;
        }

        private static string CreateKey(string source, string messageId)
        {
            return $"{source}:{messageId}";
        }
    }

    private sealed class EnqueuedMessage
    {
        public EnqueuedMessage(string topic, string source, string messageId, string payload)
        {
            this.Topic = topic;
            this.Source = source;
            this.MessageId = messageId;
            this.Payload = payload;
        }

        public string Topic { get; }

        public string Source { get; }

        public string MessageId { get; }

        public string Payload { get; }
    }
}
#pragma warning restore SA1011
