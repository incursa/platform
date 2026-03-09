namespace Incursa.Platform.CustomDomains.Tests;

[Trait("Category", "Unit")]
public sealed class CustomDomainStorageBackedServicesTests
{
    [Fact]
    public async Task UpsertDomainAsync_NormalizesHostnameAndQueriesByHostname()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var harness = new CustomDomainTestHarness();

        var upserted = await harness.Administration.UpsertDomainAsync(
            CustomDomainTestHarness.CreateDomain("domain-1", "Tenant.Example.COM."),
            cancellationToken);
        var fetched = await harness.Query.GetDomainByHostnameAsync("tenant.example.com", cancellationToken);

        upserted.Hostname.ShouldBe("tenant.example.com");
        fetched.ShouldNotBeNull();
        fetched.Id.ShouldBe(upserted.Id);
    }

    [Fact]
    public async Task UpsertDomainAsync_ProjectsExternalLinkLookups()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var harness = new CustomDomainTestHarness();

        var upserted = await harness.Administration.UpsertDomainAsync(
            CustomDomainTestHarness.CreateDomain(
                "domain-2",
                "app.example.com",
                externalLinks: [CustomDomainTestHarness.CreateCloudflareExternalLink("cf-host-1")]),
            cancellationToken);

        var fetched = await harness.Query.GetDomainByExternalLinkAsync(
            "cloudflare",
            "cf-host-1",
            "custom-hostname",
            cancellationToken);

        fetched.ShouldNotBeNull();
        fetched.Id.ShouldBe(upserted.Id);
    }

    [Fact]
    public async Task DeleteDomainAsync_RemovesCanonicalAndLookupRows()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var harness = new CustomDomainTestHarness();

        var domain = await harness.Administration.UpsertDomainAsync(
            CustomDomainTestHarness.CreateDomain(
                "domain-3",
                "delete.example.com",
                externalLinks: [CustomDomainTestHarness.CreateCloudflareExternalLink("cf-host-2")]),
            cancellationToken);

        (await harness.Administration.DeleteDomainAsync(domain.Id, cancellationToken)).ShouldBeTrue();
        (await harness.Administration.DeleteDomainAsync(domain.Id, cancellationToken)).ShouldBeFalse();
        (await harness.Query.GetDomainAsync(domain.Id, cancellationToken)).ShouldBeNull();
        (await harness.Query.GetDomainByHostnameAsync(domain.Hostname, cancellationToken)).ShouldBeNull();
        (await harness.Query.GetDomainByExternalLinkAsync("cloudflare", "cf-host-2", "custom-hostname", cancellationToken)).ShouldBeNull();
    }
}
