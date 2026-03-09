namespace Incursa.Integrations.WorkOS.Webhooks.Tests;

using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Incursa.Integrations.WorkOS.Webhooks;
using Incursa.Platform;
using Incursa.Platform.Webhooks;
using Microsoft.Extensions.DependencyInjection;

[Trait("Category", "Unit")]
public sealed class WorkOsWebhookAdapterTests
{
    private const string SigningSecret = "whsec_test_secret";

    [Fact]
    public async Task RegisteredProviderAuthenticatesValidSignatureAsync()
    {
        await using var services = BuildServices();
        var provider = services.GetServices<IWebhookProvider>().Single(static provider =>
            string.Equals(provider.Name, WorkOsWebhookDefaults.ProviderName, StringComparison.Ordinal));
        var envelope = CreateEnvelope("""
            {"id":"evt_123","event":"dsync.activated","organization_id":"org_123","created_at":"2026-03-09T06:00:00Z"}
            """);

        var result = await provider.Authenticator.AuthenticateAsync(envelope, TestContext.Current.CancellationToken);

        result.ShouldBe(new AuthResult(true, null));
    }

    [Fact]
    public async Task RegisteredProviderRejectsInvalidSignatureAsync()
    {
        await using var services = BuildServices();
        var provider = services.GetServices<IWebhookProvider>().Single(static provider =>
            string.Equals(provider.Name, WorkOsWebhookDefaults.ProviderName, StringComparison.Ordinal));
        var envelope = CreateEnvelope(
            """{"id":"evt_123","event":"dsync.activated"}""",
            headers: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [WorkOsWebhookDefaults.SignatureHeaderName] = "t=1710000000,v1=00",
            });

        var result = await provider.Authenticator.AuthenticateAsync(envelope, TestContext.Current.CancellationToken);

        result.IsAuthenticated.ShouldBeFalse();
        result.FailureReason.ShouldBe("signature_mismatch");
    }

    [Fact]
    public async Task RegisteredProviderClassifiesWorkOsPayloadAsync()
    {
        await using var services = BuildServices();
        var provider = services.GetServices<IWebhookProvider>().Single(static provider =>
            string.Equals(provider.Name, WorkOsWebhookDefaults.ProviderName, StringComparison.Ordinal));
        var envelope = CreateEnvelope("""
            {"id":"evt_123","event":"audit_log.created","organization_id":"org_456","created_at":"2026-03-09T06:30:00Z"}
            """);

        var result = await provider.Classifier.ClassifyAsync(envelope, TestContext.Current.CancellationToken);

        result.Decision.ShouldBe(WebhookIngestDecision.Accepted);
        result.ProviderEventId.ShouldBe("evt_123");
        result.EventType.ShouldBe("audit_log.created");
        result.DedupeKey.ShouldBe("evt_123");
        result.PartitionKey.ShouldBe("org_456");

        using var summary = JsonDocument.Parse(result.ParsedSummaryJson!);
        summary.RootElement.GetProperty("ProviderEventId").GetString().ShouldBe("evt_123");
        summary.RootElement.GetProperty("EventType").GetString().ShouldBe("audit_log.created");
        summary.RootElement.GetProperty("OrganizationId").GetString().ShouldBe("org_456");
    }

    [Fact]
    public async Task RegisteredProviderRejectsInvalidJsonAsync()
    {
        await using var services = BuildServices();
        var provider = services.GetServices<IWebhookProvider>().Single(static provider =>
            string.Equals(provider.Name, WorkOsWebhookDefaults.ProviderName, StringComparison.Ordinal));
        var envelope = CreateEnvelope("{not json");

        var result = await provider.Classifier.ClassifyAsync(envelope, TestContext.Current.CancellationToken);

        result.Decision.ShouldBe(WebhookIngestDecision.Rejected);
        result.FailureReason.ShouldBe("invalid_json");
    }

    [Fact]
    public async Task IngestorUsesWorkOsProviderMetadataAndHandlersAsync()
    {
        await using var services = BuildServices(registerHandler: true);
        var provider = services.GetServices<IWebhookProvider>().Single(static provider =>
            string.Equals(provider.Name, WorkOsWebhookDefaults.ProviderName, StringComparison.Ordinal));
        provider.Handlers.ShouldHaveSingleItem();

        var registry = new WebhookProviderRegistry(services.GetServices<IWebhookProvider>());
        var inbox = new FakeInbox();
        var ingestor = new WebhookIngestor(registry, inbox, new FixedTimeProvider(new DateTimeOffset(2026, 3, 9, 6, 45, 0, TimeSpan.Zero)));
        var envelope = CreateEnvelope("""
            {"id":"evt_999","event":"organization.updated","organization_id":"org_partition"}
            """);

        var result = await ingestor.IngestAsync(WorkOsWebhookDefaults.ProviderName, envelope, TestContext.Current.CancellationToken);

        result.Decision.ShouldBe(WebhookIngestDecision.Accepted);
        result.DedupeKey.ShouldBe("evt_999");
        result.PartitionKey.ShouldBe("org_partition");
        inbox.Enqueued.ShouldHaveSingleItem().MessageId.ShouldBe("evt_999");
    }

    private static ServiceProvider BuildServices(bool registerHandler = false)
    {
        var services = new ServiceCollection();
        services.AddWorkOsWebhooks(options => options.SigningSecret = SigningSecret);

        if (registerHandler)
        {
            services.AddSingleton<IWorkOsWebhookHandler, FakeWorkOsWebhookHandler>();
        }

        return services.BuildServiceProvider();
    }

    private static WebhookEnvelope CreateEnvelope(string payload, IReadOnlyDictionary<string, string>? headers = null)
    {
        var bodyBytes = Encoding.UTF8.GetBytes(payload);
        var signatureHeaders = headers is null
            ? CreateSignedHeaders(bodyBytes)
            : new Dictionary<string, string>(headers, StringComparer.OrdinalIgnoreCase);

        return new WebhookEnvelope(
            WorkOsWebhookDefaults.ProviderName,
            new DateTimeOffset(2026, 3, 9, 6, 0, 0, TimeSpan.Zero),
            "POST",
            "/webhooks/workos",
            string.Empty,
            signatureHeaders,
            "application/json",
            bodyBytes,
            "127.0.0.1");
    }

    private static Dictionary<string, string> CreateSignedHeaders(byte[] bodyBytes)
    {
        const string timestamp = "1710000000";
        var payload = Encoding.UTF8.GetBytes($"{timestamp}.{Encoding.UTF8.GetString(bodyBytes)}");
        var signature = Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(SigningSecret), payload)).ToLowerInvariant();

        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [WorkOsWebhookDefaults.SignatureHeaderName] = $"t={timestamp},v1={signature}",
        };
    }

    private sealed class FakeWorkOsWebhookHandler : IWorkOsWebhookHandler
    {
        public bool CanHandle(string eventType) => string.Equals(eventType, "organization.updated", StringComparison.Ordinal);

        public Task HandleAsync(WebhookEventContext context, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeInbox : IInbox
    {
        private readonly HashSet<string> seen = new(StringComparer.Ordinal);

        public List<EnqueuedMessage> Enqueued { get; } = [];

        public Task<bool> AlreadyProcessedAsync(string messageId, string source, CancellationToken cancellationToken)
        {
            return Task.FromResult(!seen.Add($"{source}:{messageId}"));
        }

        public Task<bool> AlreadyProcessedAsync(string messageId, string source, byte[]? hash, CancellationToken cancellationToken)
        {
            return Task.FromResult(!seen.Add($"{source}:{messageId}"));
        }

        public Task MarkProcessedAsync(string messageId, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task MarkProcessingAsync(string messageId, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task MarkDeadAsync(string messageId, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task EnqueueAsync(string topic, string source, string messageId, string payload, CancellationToken cancellationToken)
        {
            Enqueued.Add(new EnqueuedMessage(topic, source, messageId, payload));
            return Task.CompletedTask;
        }

        public Task EnqueueAsync(string topic, string source, string messageId, string payload, byte[]? hash, CancellationToken cancellationToken)
        {
            Enqueued.Add(new EnqueuedMessage(topic, source, messageId, payload));
            return Task.CompletedTask;
        }

        public Task EnqueueAsync(string topic, string source, string messageId, string payload, byte[]? hash, DateTimeOffset? dueTimeUtc, CancellationToken cancellationToken)
        {
            Enqueued.Add(new EnqueuedMessage(topic, source, messageId, payload));
            return Task.CompletedTask;
        }
    }

    private sealed record EnqueuedMessage(string Topic, string Source, string MessageId, string Payload);

    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset utcNow;

        public FixedTimeProvider(DateTimeOffset utcNow)
        {
            this.utcNow = utcNow;
        }

        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
