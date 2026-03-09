namespace Incursa.Platform.Dns.Tests;

using System.Net;
using System.Text;
using System.Text.Json;
using Incursa.Platform.Dns;
using Incursa.Integrations.Cloudflare.Dns;
using Microsoft.Extensions.DependencyInjection;

[Trait("Category", "Unit")]
public sealed class CloudflareDnsAdapterTests
{
    [Fact]
    public void AddCloudflareDns_RegistersAdapter()
    {
        var services = new ServiceCollection();
        services.AddCloudflareDns(options => options.ApiToken = "token-value");

        using var serviceProvider = services.BuildServiceProvider();
        var adapter = serviceProvider.GetRequiredService<ICloudflareDnsAdapter>();

        adapter.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetZoneAsync_MapsCloudflareZonePayload()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var handler = new StubHttpMessageHandler();
        handler.EnqueueJson("""{"success":true,"result":{"id":"cf-zone-1","name":"Example.COM."}}""");
        var adapter = CreateAdapter(handler);

        var zone = await adapter.GetZoneAsync("cf-zone-1", cancellationToken);

        zone.Id.ShouldBe(new DnsZoneId("cf-zone-1"));
        zone.Name.ShouldBe("example.com");
        zone.ExternalLinks.Count.ShouldBe(1);
        zone.ExternalLinks.Single().Provider.ShouldBe("cloudflare");
    }

    [Fact]
    public async Task ListRecordsAsync_MapsSupportedKindsAndSkipsUnsupportedKinds()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var handler = new StubHttpMessageHandler();
        handler.EnqueueJson(
            """{"success":true,"result":[{"id":"rec-a","type":"A","name":"Api.EXAMPLE.com.","content":" 192.0.2.30 ","ttl":120,"proxied":true},{"id":"rec-mx","type":"MX","name":"example.com","content":"Mail.EXAMPLE.com.","ttl":300,"priority":10},{"id":"rec-srv","type":"SRV","name":"_sip.example.com","content":"ignored","ttl":60}]}""");
        var adapter = CreateAdapter(handler);
        var zone = CreateCloudflareZone();

        var records = await adapter.ListRecordsAsync(zone, cancellationToken);

        records.Count.ShouldBe(2);
        records.Select(static item => item.Kind).OrderBy(static item => item).ShouldBe([DnsRecordKind.A, DnsRecordKind.Mx]);
        records.Single(static item => item.Kind == DnsRecordKind.A).Data.ShouldBeOfType<DnsAddressRecordData>().Address.ShouldBe("192.0.2.30");
        records.Single(static item => item.Kind == DnsRecordKind.Mx).Data.ShouldBeOfType<DnsMailExchangeRecordData>().Exchange.ShouldBe("mail.example.com");
    }

    [Fact]
    public async Task UpsertRecordAsync_SerializesProviderNeutralModel()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var handler = new StubHttpMessageHandler();
        handler.EnqueueJson(
            """{"success":true,"result":{"id":"rec-mx-1","type":"MX","name":"example.com","content":"mail.example.com","ttl":600,"priority":20,"comment":"mail route"}}""");
        var adapter = CreateAdapter(handler);
        var zone = CreateCloudflareZone();
        var record = new DnsRecord(
            new DnsRecordId("record-local-1"),
            zone.Id,
            "Example.COM.",
            DnsRecordKind.Mx,
            new DnsMailExchangeRecordData("Mail.EXAMPLE.com.", 20),
            ttl: 600,
            comment: "mail route",
            externalLinks:
            [
                new DnsExternalLink(new DnsExternalLinkId("record-link-1"), "cloudflare", "rec-mx-1", "dns-record"),
            ]);

        var upserted = await adapter.UpsertRecordAsync(zone, record, cancellationToken);
        var request = handler.Requests.Single();
        using var body = JsonDocument.Parse(request.Body!);

        request.Method.ShouldBe("PUT");
        request.RequestUri.ShouldEndWith("/zones/cf-zone-1/dns_records/rec-mx-1");
        body.RootElement.GetProperty("type").GetString().ShouldBe("MX");
        body.RootElement.GetProperty("name").GetString().ShouldBe("example.com");
        body.RootElement.GetProperty("content").GetString().ShouldBe("mail.example.com");
        body.RootElement.GetProperty("priority").GetInt32().ShouldBe(20);
        upserted.Id.ShouldBe(new DnsRecordId("cf-record:rec-mx-1"));
    }

    [Fact]
    public async Task ReconcileAsync_UpsertsDesiredRecordsAndDeletesStaleCloudflareRecords()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var handler = new StubHttpMessageHandler();
        handler.EnqueueJson(
            """{"success":true,"result":[{"id":"rec-keep","type":"A","name":"api.example.com","content":"192.0.2.55","ttl":300},{"id":"rec-delete","type":"TXT","name":"old.example.com","content":"stale","ttl":300}]}""");
        handler.EnqueueJson(
            """{"success":true,"result":{"id":"rec-keep","type":"A","name":"api.example.com","content":"192.0.2.55","ttl":300}}""");
        handler.EnqueueJson("""{"success":true,"result":{}}""");

        var adapter = CreateAdapter(handler);
        var zone = CreateCloudflareZone();
        var desired = new DnsRecord(
            new DnsRecordId("desired-a-1"),
            zone.Id,
            "API.EXAMPLE.com.",
            DnsRecordKind.A,
            new DnsAddressRecordData("192.0.2.55"));

        var result = await adapter.ReconcileAsync(zone, [desired], cancellationToken);

        result.UpsertedRecords.Count.ShouldBe(1);
        result.DeletedRecords.Count.ShouldBe(1);
        handler.Requests.Select(static item => item.Method).ShouldBe(["GET", "PUT", "DELETE"]);
        handler.Requests.Last().RequestUri.ShouldEndWith("/zones/cf-zone-1/dns_records/rec-delete");
    }

    private static ICloudflareDnsAdapter CreateAdapter(StubHttpMessageHandler handler) =>
        new CloudflareDnsAdapter(
            new HttpClient(handler)
            {
                BaseAddress = new Uri("https://api.cloudflare.com/client/v4/", UriKind.Absolute),
            },
            new CloudflareDnsOptions
            {
                ApiToken = "token-value",
                BaseUrl = new Uri("https://api.cloudflare.com/client/v4/", UriKind.Absolute),
            });

    private static DnsZone CreateCloudflareZone() =>
        new(
            new DnsZoneId("zone-local-1"),
            "example.com",
            externalLinks:
            [
                new DnsExternalLink(new DnsExternalLinkId("zone-link-1"), "cloudflare", "cf-zone-1", "zone"),
            ]);

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
