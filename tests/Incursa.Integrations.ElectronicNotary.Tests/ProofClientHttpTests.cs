namespace Incursa.Integrations.ElectronicNotary.Tests;

using System.Net;
using System.Text.Json;
using FluentAssertions;
using Incursa.Integrations.ElectronicNotary.Proof;
using Incursa.Integrations.ElectronicNotary.Proof.Contracts;
using Incursa.Integrations.ElectronicNotary.Proof.Types;
using Microsoft.Extensions.DependencyInjection;

[TestClass]
public sealed class ProofClientHttpTests
{
    [TestMethod]
    public async Task CreateTransactionPostsExpectedJsonAndApiKeyHeaderAsync()
    {
        var services = new ServiceCollection();
        using var recordingHandler = new RecordingHttpMessageHandler();
        services.AddSingleton(recordingHandler);
        services.AddProofClient(options =>
        {
            options.Environment = ProofEnvironment.Production;
            options.ApiKey = "api-key-under-test";
            options.EnableResilience = false;
        });
        services.AddHttpClient<IProofClient, ProofClient>()
            .ConfigurePrimaryHttpMessageHandler(static serviceProvider => serviceProvider.GetRequiredService<RecordingHttpMessageHandler>());

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        IProofClient client = serviceProvider.GetRequiredService<IProofClient>();

        var request = CreateTransactionRequest.Create(
            [SignerInput.Create("signer@example.com", null, null)],
            [DocumentInput.Create("https://example.test/document.pdf", ProofDocumentRequirement.Notarization)],
            false);

        Transaction response = await client.CreateTransactionAsync(request, CancellationToken.None).ConfigureAwait(false);

        response.Id.Should().Be(ProofTransactionId.Parse("ot_created123"));
        recordingHandler.Requests.Should().ContainSingle();

        CapturedRequest capturedRequest = recordingHandler.Requests.Single();
        capturedRequest.Method.Should().Be(HttpMethod.Post);
        capturedRequest.RequestUri.Should().Be(new Uri("https://api.proof.com/transactions"));
        capturedRequest.Headers.TryGetValue("ApiKey", out string? apiKey).Should().BeTrue();
        apiKey.Should().Be("api-key-under-test");

        using JsonDocument payload = JsonDocument.Parse(capturedRequest.Body);
        JsonElement root = payload.RootElement;
        root.GetProperty("signers")[0].GetProperty("email").GetString().Should().Be("signer@example.com");
        root.GetProperty("documents")[0].GetProperty("resource").GetString().Should().Be("https://example.test/document.pdf");
        root.GetProperty("documents")[0].GetProperty("requirement").GetString().Should().Be("notarization");
    }

    [TestMethod]
    public async Task NonSuccessResponseThrowsProofApiExceptionWithCorrelationAsync()
    {
        var services = new ServiceCollection();
        using var recordingHandler = new RecordingHttpMessageHandler();
        recordingHandler.QueueResponse(static () =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests)
            {
                Content = new StringContent("""{"error":"rate_limited"}"""),
            };
            response.Headers.Add("X-Correlation-ID", "corr-123");
            return response;
        });
        services.AddSingleton(recordingHandler);
        services.AddProofClient(options =>
        {
            options.Environment = ProofEnvironment.Production;
            options.ApiKey = "api-key-under-test";
            options.EnableResilience = false;
        });
        services.AddHttpClient<IProofClient, ProofClient>()
            .ConfigurePrimaryHttpMessageHandler(static serviceProvider => serviceProvider.GetRequiredService<RecordingHttpMessageHandler>());

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        IProofClient client = serviceProvider.GetRequiredService<IProofClient>();

        Func<Task> act = async () => await client.GetTransactionAsync(ProofTransactionId.Parse("ot_created123"), CancellationToken.None).ConfigureAwait(false);

        ProofApiException exception = (await act.Should().ThrowAsync<ProofApiException>().ConfigureAwait(false)).Which;
        exception.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        exception.ResponseBody.Should().Contain("rate_limited");
        exception.CorrelationInfo.Should().Be("X-Correlation-ID=corr-123");
    }

    [TestMethod]
    public async Task CreateTransactionRegistersTransactionIdWithSinkAsync()
    {
        var services = new ServiceCollection();
        using var recordingHandler = new RecordingHttpMessageHandler();
        var registrationSink = new RecordingTransactionRegistrationSink();
        services.AddSingleton(recordingHandler);
        services.AddSingleton<IProofTransactionRegistrationSink>(registrationSink);
        services.AddProofClient(options =>
        {
            options.Environment = ProofEnvironment.Production;
            options.ApiKey = "api-key-under-test";
        });
        services.AddHttpClient<IProofClient, ProofClient>()
            .ConfigurePrimaryHttpMessageHandler(static serviceProvider => serviceProvider.GetRequiredService<RecordingHttpMessageHandler>());

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        IProofClient client = serviceProvider.GetRequiredService<IProofClient>();

        var request = CreateTransactionRequest.Create(
            [SignerInput.Create("signer@example.com", null, null)],
            [DocumentInput.Create("https://example.test/document.pdf", ProofDocumentRequirement.Notarization)],
            false);

        _ = await client.CreateTransactionAsync(request, CancellationToken.None).ConfigureAwait(false);

        registrationSink.RegisteredTransactionIds.Should().ContainSingle();
        registrationSink.RegisteredTransactionIds.Single().Should().Be(ProofTransactionId.Parse("ot_created123"));
    }

    [TestMethod]
    public async Task GetTransactionRetriesTransientStatusCodesAsync()
    {
        var services = new ServiceCollection();
        using var recordingHandler = new RecordingHttpMessageHandler();
        recordingHandler.QueueResponse(static () => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
        {
            Content = new StringContent("""{"error":"temporarily_unavailable"}"""),
        });
        recordingHandler.QueueResponse(static () => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"id":"ot_retry123"}"""),
        });
        services.AddSingleton(recordingHandler);
        services.AddProofClient(options =>
        {
            options.Environment = ProofEnvironment.Production;
            options.ApiKey = "api-key-under-test";
            options.MaxRetryAttempts = 2;
            options.InitialBackoff = TimeSpan.FromMilliseconds(1);
            options.MaxBackoff = TimeSpan.FromMilliseconds(2);
        });
        services.AddHttpClient<IProofClient, ProofClient>()
            .ConfigurePrimaryHttpMessageHandler(static serviceProvider => serviceProvider.GetRequiredService<RecordingHttpMessageHandler>());

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        IProofClient client = serviceProvider.GetRequiredService<IProofClient>();

        Transaction response = await client.GetTransactionAsync(ProofTransactionId.Parse("ot_retry123"), CancellationToken.None).ConfigureAwait(false);

        response.Id.Should().Be(ProofTransactionId.Parse("ot_retry123"));
        recordingHandler.Requests.Count.Should().Be(2);
    }

    [TestMethod]
    public async Task CircuitBreakerRejectsRequestsAfterConsecutiveTransientFailuresAsync()
    {
        var services = new ServiceCollection();
        using var recordingHandler = new RecordingHttpMessageHandler();
        recordingHandler.QueueResponse(static () => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
        {
            Content = new StringContent("""{"error":"temporarily_unavailable"}"""),
        });
        recordingHandler.QueueResponse(static () => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
        {
            Content = new StringContent("""{"error":"temporarily_unavailable"}"""),
        });
        services.AddSingleton(recordingHandler);
        services.AddProofClient(options =>
        {
            options.Environment = ProofEnvironment.Production;
            options.ApiKey = "api-key-under-test";
            options.MaxRetryAttempts = 0;
            options.CircuitBreakerFailureThreshold = 2;
            options.CircuitBreakDuration = TimeSpan.FromMinutes(1);
        });
        services.AddHttpClient<IProofClient, ProofClient>()
            .ConfigurePrimaryHttpMessageHandler(static serviceProvider => serviceProvider.GetRequiredService<RecordingHttpMessageHandler>());

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        IProofClient client = serviceProvider.GetRequiredService<IProofClient>();
        ProofTransactionId transactionId = ProofTransactionId.Parse("ot_retry123");

        Func<Task> firstCall = async () => await client.GetTransactionAsync(transactionId, CancellationToken.None).ConfigureAwait(false);
        Func<Task> secondCall = async () => await client.GetTransactionAsync(transactionId, CancellationToken.None).ConfigureAwait(false);
        Func<Task> thirdCall = async () => await client.GetTransactionAsync(transactionId, CancellationToken.None).ConfigureAwait(false);

        await firstCall.Should().ThrowAsync<ProofApiException>().ConfigureAwait(false);
        await secondCall.Should().ThrowAsync<ProofApiException>().ConfigureAwait(false);
        await thirdCall.Should().ThrowAsync<HttpRequestException>().ConfigureAwait(false);

        recordingHandler.Requests.Count.Should().Be(2);
    }

    private sealed class RecordingHttpMessageHandler : HttpMessageHandler
    {
        private readonly Queue<Func<HttpResponseMessage>> queuedResponses = new Queue<Func<HttpResponseMessage>>();

        public List<CapturedRequest> Requests { get; } = new List<CapturedRequest>();

        public void QueueResponse(Func<HttpResponseMessage> responseFactory)
        {
            this.queuedResponses.Enqueue(responseFactory);
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string body = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (KeyValuePair<string, IEnumerable<string>> header in request.Headers)
            {
                headers[header.Key] = string.Join(",", header.Value);
            }

            this.Requests.Add(new CapturedRequest(request.Method, request.RequestUri, headers, body));

            if (this.queuedResponses.Count > 0)
            {
                return this.queuedResponses.Dequeue().Invoke();
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"id":"ot_created123"}"""),
            };
        }
    }

    private sealed class CapturedRequest
    {
        public CapturedRequest(HttpMethod method, Uri? requestUri, IReadOnlyDictionary<string, string> headers, string body)
        {
            this.Method = method;
            this.RequestUri = requestUri;
            this.Headers = headers;
            this.Body = body;
        }

        public HttpMethod Method { get; }

        public Uri? RequestUri { get; }

        public IReadOnlyDictionary<string, string> Headers { get; }

        public string Body { get; }
    }

    private sealed class RecordingTransactionRegistrationSink : IProofTransactionRegistrationSink
    {
        public List<ProofTransactionId> RegisteredTransactionIds { get; } = new List<ProofTransactionId>();

        public Task RegisterTransactionAsync(ProofTransactionId transactionId, CancellationToken cancellationToken)
        {
            _ = cancellationToken;
            this.RegisteredTransactionIds.Add(transactionId);
            return Task.CompletedTask;
        }
    }
}
