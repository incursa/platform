namespace Incursa.Platform.Dns.Tests;

using Incursa.Platform.Dns;

[Trait("Category", "Unit")]
public sealed class DnsStorageBackedServicesTests
{
    [Fact]
    public async Task UpsertZoneAsync_NormalizesZoneAndQueriesByOwner()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var harness = new DnsTestHarness();

        var zone = DnsTestHarness.CreateZone("zone-1", "Example.COM.", " Team-A ");

        var upserted = await harness.Zones.UpsertZoneAsync(zone, cancellationToken);
        var fetched = await harness.Query.GetZoneAsync(zone.Id, cancellationToken);
        var byOwner = await DnsTestHarness.ToListAsync(
            harness.Query.QueryZonesByOwnerAsync("team-a", cancellationToken),
            cancellationToken);

        upserted.Name.ShouldBe("example.com");
        fetched.ShouldNotBeNull();
        fetched.Name.ShouldBe("example.com");
        byOwner.Select(static item => item.Id).ShouldContain(zone.Id);
    }

    [Fact]
    public async Task UpsertZoneAsync_PreservesMetadataWhenUpdateOmitsIt()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var harness = new DnsTestHarness();
        var createdUtc = new DateTimeOffset(2026, 3, 8, 12, 0, 0, TimeSpan.Zero);
        var externalLink = new DnsExternalLink(new DnsExternalLinkId("zone-link-1"), "cloudflare", "cf-zone-1", "zone");

        await harness.Zones.UpsertZoneAsync(
            DnsTestHarness.CreateZone("zone-2", "example.org", "team-b", createdUtc, [externalLink]),
            cancellationToken);

        var updated = await harness.Zones.UpsertZoneAsync(
            DnsTestHarness.CreateZone("zone-2", "Example.ORG.", "team-b"),
            cancellationToken);

        updated.CreatedUtc.ShouldBe(createdUtc);
        updated.ExternalLinks.ShouldContain(externalLink);
    }

    [Fact]
    public async Task UpsertRecordAsync_NormalizesDataAndSupportsKindOnlyQueries()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var harness = new DnsTestHarness();
        var zone = await harness.Zones.UpsertZoneAsync(
            DnsTestHarness.CreateZone("zone-3", "example.net"),
            cancellationToken);

        await harness.Records.UpsertRecordAsync(
            DnsTestHarness.CreateRecord(
                "record-a-1",
                zone.Id.Value,
                "Api.EXAMPLE.net.",
                DnsRecordKind.A,
                new DnsAddressRecordData(" 192.0.2.10 ")),
            cancellationToken);

        var cname = await harness.Records.UpsertRecordAsync(
            DnsTestHarness.CreateRecord(
                "record-cname-1",
                zone.Id.Value,
                "WWW.Example.NET.",
                DnsRecordKind.CName,
                new DnsCanonicalNameRecordData("Target.EXAMPLE.net.")),
            cancellationToken);

        var aRecords = await DnsTestHarness.ToListAsync(
            harness.Query.QueryRecordsAsync(new DnsRecordQuery(zone.Id, Kind: DnsRecordKind.A), cancellationToken),
            cancellationToken);

        aRecords.Count.ShouldBe(1);
        aRecords.Single().Name.ShouldBe("api.example.net");
        cname.Name.ShouldBe("www.example.net");
        cname.Data.ShouldBeOfType<DnsCanonicalNameRecordData>().CanonicalName.ShouldBe("target.example.net");
    }

    [Fact]
    public async Task DeleteRecordAsync_RemovesCanonicalAndProjectionRows()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var harness = new DnsTestHarness();
        var zone = await harness.Zones.UpsertZoneAsync(
            DnsTestHarness.CreateZone("zone-4", "example.delete"),
            cancellationToken);

        var record = await harness.Records.UpsertRecordAsync(
            DnsTestHarness.CreateRecord(
                "record-delete-1",
                zone.Id.Value,
                "app.example.delete",
                DnsRecordKind.Txt,
                new DnsTextRecordData("token")),
            cancellationToken);

        var deleted = await harness.Records.DeleteRecordAsync(record.Id, cancellationToken);
        var fetched = await harness.Query.GetRecordAsync(record.Id, cancellationToken);
        var records = await DnsTestHarness.ToListAsync(harness.Query.GetRecordsAsync(zone.Id, cancellationToken), cancellationToken);

        deleted.ShouldBeTrue();
        fetched.ShouldBeNull();
        records.ShouldBeEmpty();
    }

    [Fact]
    public async Task ReconcileAsync_UpsertsDesiredRecordsAndDeletesMissingRecords()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var harness = new DnsTestHarness();
        var zone = await harness.Zones.UpsertZoneAsync(
            DnsTestHarness.CreateZone("zone-5", "example.reconcile"),
            cancellationToken);
        var existingA = await harness.Records.UpsertRecordAsync(
            DnsTestHarness.CreateRecord(
                "record-keep-1",
                zone.Id.Value,
                "api.example.reconcile",
                DnsRecordKind.A,
                new DnsAddressRecordData("192.0.2.20")),
            cancellationToken);
        _ = await harness.Records.UpsertRecordAsync(
            DnsTestHarness.CreateRecord(
                "record-delete-2",
                zone.Id.Value,
                "old.example.reconcile",
                DnsRecordKind.Txt,
                new DnsTextRecordData("stale")),
            cancellationToken);

        var result = await harness.Records.ReconcileAsync(
            zone.Id,
            [
                DnsTestHarness.CreateRecord(
                    "desired-a-1",
                    zone.Id.Value,
                    "API.example.reconcile.",
                    DnsRecordKind.A,
                    new DnsAddressRecordData("192.0.2.20")),
                DnsTestHarness.CreateRecord(
                    "desired-mx-1",
                    zone.Id.Value,
                    "example.reconcile",
                    DnsRecordKind.Mx,
                    new DnsMailExchangeRecordData("MAIL.example.reconcile.", 10)),
            ],
            cancellationToken);

        var finalRecords = await DnsTestHarness.ToListAsync(harness.Query.GetRecordsAsync(zone.Id, cancellationToken), cancellationToken);

        result.UpsertedRecords.Count.ShouldBe(2);
        result.UpsertedRecords.Select(static item => item.Id).ShouldContain(existingA.Id);
        result.DeletedRecords.Count.ShouldBe(1);
        result.DeletedRecords.Single().Name.ShouldBe("old.example.reconcile");
        finalRecords.Count.ShouldBe(2);
        finalRecords.Select(static item => item.Kind).OrderBy(static item => item).ShouldBe([DnsRecordKind.A, DnsRecordKind.Mx]);
    }
}
