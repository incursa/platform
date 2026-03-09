using Incursa.Integrations.Cloudflare.Clients;
using Incursa.Integrations.Cloudflare.Internal;
using Incursa.Integrations.Cloudflare.Options;
using Microsoft.Extensions.Logging.Abstractions;

namespace Incursa.Integrations.Cloudflare.IntegrationTests;

public sealed class CloudflareKvLiveIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task KvRoundTrip_PutGetDeleteAsync()
    {
        var hasApiToken = TryGetEnv("CF_TEST_API_TOKEN", out var apiToken);
        var hasAccountId = TryGetEnv("CF_TEST_ACCOUNT_ID", out var accountId);
        var hasNamespaceId = TryGetEnv("CF_TEST_KV_NAMESPACE_ID", out var namespaceId);
        var hasRequiredEnv = hasApiToken && hasAccountId && hasNamespaceId;

        if (!hasRequiredEnv)
        {
            return;
        }

        var baseUrl = Environment.GetEnvironmentVariable("CF_TEST_BASE_URL");
        var resolvedBaseUrl = string.IsNullOrWhiteSpace(baseUrl)
            ? new Uri("https://api.cloudflare.com/client/v4", UriKind.Absolute)
            : new Uri(baseUrl, UriKind.Absolute);

        using HttpClient httpClient = new HttpClient
        {
            BaseAddress = new Uri($"{resolvedBaseUrl.AbsoluteUri.TrimEnd('/')}/", UriKind.Absolute),
        };

        var transport = new CloudflareApiTransport(
            httpClient,
            Microsoft.Extensions.Options.Options.Create(new CloudflareApiOptions
            {
                BaseUrl = resolvedBaseUrl,
                ApiToken = apiToken,
                AccountId = accountId,
                RetryCount = 1,
            }),
            NullLogger<CloudflareApiTransport>.Instance);

        var client = new CloudflareKvClient(
            transport,
            Microsoft.Extensions.Options.Options.Create(new CloudflareApiOptions { AccountId = accountId }),
            Microsoft.Extensions.Options.Options.Create(new CloudflareKvOptions { NamespaceId = namespaceId }));

        var key = $"integration/{Guid.NewGuid():N}";
        var value = $"v-{Guid.NewGuid():N}";

        await client.PutAsync(key, value, CancellationToken.None);
        var roundTrip = await client.GetAsync(key, CancellationToken.None);
        Assert.Equal(value, roundTrip);

        await client.DeleteAsync(key, CancellationToken.None);
        var missing = await client.GetAsync(key, CancellationToken.None);
        Assert.Null(missing);
    }

    private static bool TryGetEnv(string name, out string value)
    {
        var maybe = Environment.GetEnvironmentVariable(name);
        value = maybe ?? string.Empty;
        return !string.IsNullOrWhiteSpace(maybe);
    }
}
