using Incursa.Integrations.Cloudflare.Clients;
using Incursa.Integrations.Cloudflare.DependencyInjection;
using Incursa.Integrations.Cloudflare.Options;
using Incursa.Integrations.Cloudflare.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Incursa.Integrations.Cloudflare.Tests;

public sealed class CloudflareDependencyInjectionTests
{
    [Fact]
    public void AddCloudflareIntegration_RegistersCoreServices()
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddCloudflareIntegration(options =>
        {
            options.ApiToken = "token";
            options.BaseUrl = new Uri("https://api.cloudflare.com/client/v4", UriKind.Absolute);
            options.AccountId = "acct";
            options.ZoneId = "zone";
            options.ForceIpv4 = false;
        });
        services.AddCloudflareKv(options =>
        {
            options.AccountId = "acct";
            options.NamespaceId = "ns";
        });
        services.AddCloudflareR2(options =>
        {
            options.Endpoint = "https://example.r2.cloudflarestorage.com";
            options.AccessKeyId = "key";
            options.SecretAccessKey = "secret";
            options.Bucket = "bucket";
        });

        using var provider = services.BuildServiceProvider(validateScopes: true);
        Assert.NotNull(provider.GetService<ICloudflareKvClient>());
        Assert.NotNull(provider.GetService<ICloudflareKvStore>());
    }

    [Fact]
    public void AddCloudflareIntegration_BindsConfigurationSections()
    {
        var values = new Dictionary<string, string?>
        {
            ["Cloudflare:ApiToken"] = "token",
            ["Cloudflare:AccountId"] = "acct",
            ["Cloudflare:ZoneId"] = "zone",
            ["Cloudflare:BaseUrl"] = "https://api.cloudflare.com/client/v4",
            ["Cloudflare:ForceIpv4"] = "false",
            ["Cloudflare:KV:NamespaceId"] = "ns",
            ["Cloudflare:R2:Bucket"] = "bucket",
        };

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();

        ServiceCollection services = new();
        services.AddLogging();
        services.AddCloudflareIntegration(configuration);

        using var provider = services.BuildServiceProvider(validateScopes: true);
        var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<CloudflareApiOptions>>().Value;

        Assert.Equal("token", options.ApiToken);
        Assert.Equal("api.cloudflare.com", options.BaseUrl.Host);
        Assert.False(options.ForceIpv4);
    }

    [Fact]
    public void CloudflareApiOptions_ForceIpv4_DefaultsToTrue()
    {
        var options = new CloudflareApiOptions();
        Assert.True(options.ForceIpv4);
    }

    [Fact]
    public void AddCloudflareInMemoryStorage_ReplacesCloudflareStorageRegistrations()
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddCloudflareIntegration(options =>
        {
            options.ApiToken = "token";
            options.BaseUrl = new Uri("https://api.cloudflare.com/client/v4", UriKind.Absolute);
            options.AccountId = "acct";
            options.ZoneId = "zone";
        });
        services.AddCloudflareKv(options =>
        {
            options.AccountId = "acct";
            options.NamespaceId = "ns";
        });
        services.AddCloudflareR2(options =>
        {
            options.Endpoint = "https://example.r2.cloudflarestorage.com";
            options.AccessKeyId = "key";
            options.SecretAccessKey = "secret";
            options.Bucket = "bucket";
        });

        services.AddCloudflareInMemoryStorage();

        using var provider = services.BuildServiceProvider(validateScopes: true);
        var kv = provider.GetRequiredService<ICloudflareKvStore>();
        var r2 = provider.GetRequiredService<ICloudflareR2BlobStore>();

        Assert.IsType<InMemoryCloudflareKvStore>(kv);
        Assert.IsType<InMemoryCloudflareR2BlobStore>(r2);
    }

    [Fact]
    public void AddCloudflareInMemoryStorage_CanBeCalledWithoutCloudflareIntegration()
    {
        ServiceCollection services = new();
        services.AddCloudflareInMemoryStorage();

        using var provider = services.BuildServiceProvider(validateScopes: true);
        Assert.IsType<InMemoryCloudflareKvStore>(provider.GetRequiredService<ICloudflareKvStore>());
        Assert.IsType<InMemoryCloudflareR2BlobStore>(provider.GetRequiredService<ICloudflareR2BlobStore>());
    }
}
