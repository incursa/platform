namespace Incursa.Platform.CustomDomains.Tests;

using System.Net;
using System.Text;
using Incursa.Integrations.Cloudflare.CustomDomains;

[Trait("Category", "Unit")]
public sealed class CloudflareCustomDomainSynchronizationTests
{
    [Fact]
    public async Task EnsureDomainAsync_UsesExistingProviderDomain_WhenHostnameAlreadyExists()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var harness = new CustomDomainTestHarness();
        using var handler = new StubHttpMessageHandler();
        handler.EnqueueJson(
            """
            {"success":true,"result":[{"id":"cf-host-1","hostname":"tenant.example.com","status":"active","ssl":{"status":"active","method":"http"},"ownership_verification":{"name":"_cf-custom-hostname.tenant.example.com","type":"txt","value":"proof-1"}}]}
            """);

        var service = CustomDomainTestHarness.CreateCloudflareSynchronizationService(
            new HttpClient(handler),
            harness.Administration,
            harness.Query);

        var domain = await service.EnsureDomainAsync("Tenant.Example.COM.", cancellationToken);
        var fetched = await harness.Query.GetDomainByHostnameAsync("tenant.example.com", cancellationToken);

        handler.Requests.Select(static item => item.Method).ShouldBe(["GET"]);
        domain.LifecycleStatus.ShouldBe(CustomDomainLifecycleStatus.Active);
        domain.CertificateStatus.ShouldBe(CustomDomainCertificateStatus.Active);
        domain.OwnershipVerification.ShouldNotBeNull();
        fetched.ShouldNotBeNull();
        fetched.Id.ShouldBe(domain.Id);
    }

    [Fact]
    public async Task EnsureDomainAsync_CreatesProviderDomain_WhenMissing()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var harness = new CustomDomainTestHarness();
        using var handler = new StubHttpMessageHandler();
        handler.EnqueueJson("""{"success":true,"result":[]}""");
        handler.EnqueueJson(
            """
            {"success":true,"result":{"id":"cf-host-2","hostname":"new.example.com","status":"pending","ssl":{"status":"pending_validation","method":"http"},"ownership_verification":{"name":"_cf-custom-hostname.new.example.com","type":"txt","value":"proof-2"}}}
            """);

        var service = CustomDomainTestHarness.CreateCloudflareSynchronizationService(
            new HttpClient(handler),
            harness.Administration,
            harness.Query);

        var domain = await service.EnsureDomainAsync("NEW.EXAMPLE.COM.", cancellationToken);
        var persisted = await harness.Query.GetDomainByExternalLinkAsync(
            "cloudflare",
            "cf-host-2",
            "custom-hostname",
            cancellationToken);

        handler.Requests.Select(static item => item.Method).ShouldBe(["GET", "POST"]);
        handler.Requests[1].RequestUri.ShouldEndWith("/zones/zone-123/custom_hostnames");
        handler.Requests[1].Body.ShouldNotBeNull();
        handler.Requests[1].Body!.ShouldContain("\"hostname\":\"new.example.com\"");
        domain.LifecycleStatus.ShouldBe(CustomDomainLifecycleStatus.Pending);
        domain.CertificateStatus.ShouldBe(CustomDomainCertificateStatus.Pending);
        persisted.ShouldNotBeNull();
        persisted.Id.ShouldBe(domain.Id);
    }

    [Fact]
    public async Task SyncByExternalIdAsync_ReusesExistingLocalDomainId()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var harness = new CustomDomainTestHarness();
        var existing = await harness.Administration.UpsertDomainAsync(
            CustomDomainTestHarness.CreateDomain(
                "domain-local-1",
                "sync.example.com",
                externalLinks: [CustomDomainTestHarness.CreateCloudflareExternalLink("cf-host-3")]),
            cancellationToken);

        using var handler = new StubHttpMessageHandler();
        handler.EnqueueJson(
            """
            {"success":true,"result":{"id":"cf-host-3","hostname":"sync.example.com","status":"blocked","ssl":{"status":"failed","method":"http","validation_errors":[{"message":"validation failed"}]}}}
            """);

        var service = CustomDomainTestHarness.CreateCloudflareSynchronizationService(
            new HttpClient(handler),
            harness.Administration,
            harness.Query);

        var domain = await service.SyncByExternalIdAsync("cf-host-3", cancellationToken);

        domain.ShouldNotBeNull();
        domain.Id.ShouldBe(existing.Id);
        domain.LifecycleStatus.ShouldBe(CustomDomainLifecycleStatus.Failed);
        domain.CertificateStatus.ShouldBe(CustomDomainCertificateStatus.Failed);
        domain.LastError.ShouldBe("validation failed");
    }

    internal sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> responses = new();

        public List<HttpRequestSnapshot> Requests { get; } = [];

        public void EnqueueJson(string json, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            responses.Enqueue(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            });
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var body = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            Requests.Add(new HttpRequestSnapshot(request.Method.Method, request.RequestUri!.ToString(), body));
            if (!responses.TryDequeue(out var response))
            {
                throw new InvalidOperationException("No queued response is available.");
            }

            return response;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                while (responses.Count > 0)
                {
                    responses.Dequeue().Dispose();
                }
            }

            base.Dispose(disposing);
        }
    }

    internal sealed record HttpRequestSnapshot(string Method, string RequestUri, string? Body);
}
