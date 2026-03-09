namespace Incursa.Integrations.WorkOS.Tests;

using System.Net;
using Incursa.Integrations.WorkOS.Abstractions.Audit;
using Incursa.Integrations.WorkOS.Abstractions.Configuration;
using Incursa.Integrations.WorkOS.Core.Clients;

[TestClass]
public sealed class WorkOsAuditHttpClientTests
{
    [TestMethod]
    public async Task CreateEventAsync_Success_SendsExpectedPayloadAndHeader()
    {
        var handler = new StubHandler(async (request, _) =>
        {
            Assert.AreEqual(HttpMethod.Post, request.Method);
            Assert.AreEqual("/audit_logs/events", request.RequestUri?.AbsolutePath);
            Assert.AreEqual("test-idempotency-key", request.Headers.GetValues("Idempotency-Key").Single());

            var body = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync().ConfigureAwait(false);
            Assert.IsTrue(body.Contains("\"organization_id\":\"org_123\"", StringComparison.Ordinal));
            Assert.IsTrue(body.Contains("\"action\":\"user.signed_in\"", StringComparison.Ordinal));
            Assert.IsTrue(body.Contains("\"targets\"", StringComparison.Ordinal));

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"id\":\"event_123\"}", Encoding.UTF8, "application/json"),
            };
        });

        var sut = CreateClient(handler);
        var request = new WorkOsAuditCreateEventRequest(
            OrganizationId: "org_123",
            Action: "user.signed_in",
            OccurredAtUtc: DateTimeOffset.UtcNow,
            Actor: new WorkOsAuditActor("user_123", "user"),
            Targets: [new WorkOsAuditTarget("team_1", "team")],
            Context: new WorkOsAuditContext("127.0.0.1", "UnitTest"),
            Metadata: new Dictionary<string, string> { ["environment"] = "test" },
            Version: 2,
            IdempotencyKey: "test-idempotency-key");

        var result = await sut.CreateEventAsync(request).ConfigureAwait(false);

        Assert.AreEqual("event_123", result.EventId);
    }

    [TestMethod]
    public async Task CreateEventAsync_TooManyRequests_ThrowsTransientFailure()
    {
        var handler = new StubHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.TooManyRequests)
        {
            Content = new StringContent("{\"error\":\"rate_limited\"}", Encoding.UTF8, "application/json"),
        }));

        var sut = CreateClient(handler);
        var request = CreateRequest();

        WorkOsAuditClientException? ex = null;
        try
        {
            _ = await sut.CreateEventAsync(request).ConfigureAwait(false);
        }
        catch (WorkOsAuditClientException caught)
        {
            ex = caught;
        }

        Assert.IsNotNull(ex);

        Assert.AreEqual(WorkOsAuditFailureKind.Transient, ex!.FailureKind);
        Assert.AreEqual(429, ex.StatusCode);
    }

    [TestMethod]
    public async Task CreateEventAsync_BadRequest_ThrowsPermanentFailure()
    {
        var handler = new StubHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("{\"error\":\"invalid_request\"}", Encoding.UTF8, "application/json"),
        }));

        var sut = CreateClient(handler);
        var request = CreateRequest();

        WorkOsAuditClientException? ex = null;
        try
        {
            _ = await sut.CreateEventAsync(request).ConfigureAwait(false);
        }
        catch (WorkOsAuditClientException caught)
        {
            ex = caught;
        }

        Assert.IsNotNull(ex);

        Assert.AreEqual(WorkOsAuditFailureKind.Permanent, ex!.FailureKind);
        Assert.AreEqual(400, ex.StatusCode);
    }

    private static WorkOsAuditCreateEventRequest CreateRequest()
    {
        return new WorkOsAuditCreateEventRequest(
            OrganizationId: "org_123",
            Action: "user.updated",
            OccurredAtUtc: DateTimeOffset.UtcNow,
            Actor: new WorkOsAuditActor("user_123", "user"),
            Targets: [new WorkOsAuditTarget("user_123", "user")],
            Context: null,
            Metadata: null,
            Version: null,
            IdempotencyKey: "idempotency-123");
    }

    private static WorkOsAuditHttpClient CreateClient(HttpMessageHandler handler)
    {
        return new WorkOsAuditHttpClient(
            new HttpClient(handler),
            new WorkOsManagementOptions
            {
                BaseUrl = "https://api.workos.test",
                ApiKey = "sk_test_123",
                RequestTimeout = TimeSpan.FromSeconds(5),
            });
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

        public StubHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => _handler(request, cancellationToken);
    }
}
