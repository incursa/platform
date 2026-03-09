namespace Incursa.Integrations.WorkOS.Tests;

using System.Net;
using Incursa.Integrations.WorkOS.Abstractions.Configuration;
using Incursa.Integrations.WorkOS.Abstractions.Management;
using Incursa.Integrations.WorkOS.Abstractions.Mapping;
using Incursa.Integrations.WorkOS.Core.Clients;

[TestClass]
public sealed class WorkOsManagementHttpClientTests
{
    [TestMethod]
    public async Task ValidateApiKeyAsync_SuccessfulPayload_ParsesPermissions()
    {
        var handler = new StubHandler(async (request, _) =>
        {
            Assert.AreEqual(HttpMethod.Post, request.Method);
            Assert.AreEqual("/api_keys/validations", request.RequestUri?.AbsolutePath);

            var body = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync().ConfigureAwait(false);
            Assert.IsTrue(body.Contains("\"value\":\"secret\"", StringComparison.Ordinal));

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"api_key\":{\"id\":\"key_1\",\"name\":\"integration\",\"owner\":{\"type\":\"organization\",\"id\":\"org_1\"},\"permissions\":[\"nuget.read\",\"raw.push\"]}}", Encoding.UTF8, "application/json"),
            };
        });

        var sut = CreateClient(handler, new FakeTenantMapper("tenant-1"));

        var result = await sut.ValidateApiKeyAsync("secret").ConfigureAwait(false);

        Assert.IsNotNull(result);
        Assert.AreEqual("key_1", result.ApiKeyId);
        Assert.AreEqual("org_1", result.OrganizationId);
        CollectionAssert.AreEquivalent(new[] { "nuget.read", "raw.push" }, result.Permissions.ToArray());
    }

    [TestMethod]
    public async Task ValidateApiKeyAsync_WhenApiKeyIsNull_ReturnsNull()
    {
        var handler = new StubHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"api_key\":null}", Encoding.UTF8, "application/json"),
        }));

        var sut = CreateClient(handler, new FakeTenantMapper("tenant-1"));

        var result = await sut.ValidateApiKeyAsync("secret").ConfigureAwait(false);

        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task CreateApiKeyAsync_SetsNormalizedScopeOrderAndTenant()
    {
        var handler = new StubHandler((request, _) =>
        {
            if (request.Method == HttpMethod.Post && request.RequestUri?.AbsolutePath.EndsWith("/api_keys", StringComparison.Ordinal) == true)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"id\":\"key_1\",\"name\":\"k1\",\"secret\":\"sk_test\"}", Encoding.UTF8, "application/json"),
                });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        });

        var sut = CreateClient(handler, new FakeTenantMapper("tenant-1"));

        var created = await sut.CreateApiKeyAsync("org_1", "k1", ["raw.push", "nuget.read", "raw.push"], ttlHours: 2).ConfigureAwait(false);

        Assert.AreEqual("tenant-1", created.TenantId);
        CollectionAssert.AreEqual(new[] { "nuget.read", "raw.push" }, created.EffectiveScopes.ToArray());
        Assert.AreEqual("sk_test", created.Secret);
    }

    [TestMethod]
    public async Task CreateApiKeyAsync_UnwrapsDataAndReadsOneTimeTokenSecret()
    {
        var handler = new StubHandler((request, _) =>
        {
            if (request.Method == HttpMethod.Post && request.RequestUri?.AbsolutePath.EndsWith("/api_keys", StringComparison.Ordinal) == true)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Created)
                {
                    Content = new StringContent("{\"data\":{\"id\":\"key_2\",\"name\":\"k2\",\"one_time_token\":{\"secret\":\"sk_once\"}}}", Encoding.UTF8, "application/json"),
                });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        });

        var sut = CreateClient(handler, new FakeTenantMapper("tenant-2"));

        var created = await sut.CreateApiKeyAsync("org_2", "k2", ["raw.push"], ttlHours: null).ConfigureAwait(false);

        Assert.AreEqual("key_2", created.ApiKeyId);
        Assert.AreEqual("tenant-2", created.TenantId);
        Assert.AreEqual("sk_once", created.Secret);
    }

    [TestMethod]
    public async Task CreateApiKeyAsync_ReadsTopLevelValueAsSecret()
    {
        var handler = new StubHandler((request, _) =>
        {
            if (request.Method == HttpMethod.Post && request.RequestUri?.AbsolutePath.EndsWith("/api_keys", StringComparison.Ordinal) == true)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Created)
                {
                    Content = new StringContent("{\"id\":\"key_3\",\"name\":\"k3\",\"value\":\"sk_value\"}", Encoding.UTF8, "application/json"),
                });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        });

        var sut = CreateClient(handler, new FakeTenantMapper("tenant-3"));

        var created = await sut.CreateApiKeyAsync("org_3", "k3", ["raw.push"], ttlHours: null).ConfigureAwait(false);

        Assert.AreEqual("key_3", created.ApiKeyId);
        Assert.AreEqual("sk_value", created.Secret);
    }

    [TestMethod]
    public async Task GetApiKeyAsync_NonSuccess_ReturnsNull()
    {
        var handler = new StubHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));
        var sut = CreateClient(handler, new FakeTenantMapper("tenant-1"));

        var result = await sut.GetApiKeyAsync("org_1", "missing").ConfigureAwait(false);

        Assert.IsNull(result);
    }

    private static WorkOsManagementHttpClient CreateClient(HttpMessageHandler handler, IWorkOsTenantMapper mapper)
    {
        return new WorkOsManagementHttpClient(
            new HttpClient(handler),
            new WorkOsManagementOptions
            {
                BaseUrl = "https://api.workos.test",
                ApiKey = "sk_test_123",
                RequestTimeout = TimeSpan.FromSeconds(5),
            },
            mapper);
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

    private sealed class FakeTenantMapper : IWorkOsTenantMapper
    {
        private readonly string _tenant;

        public FakeTenantMapper(string tenant)
        {
            _tenant = tenant;
        }

        public ValueTask<string?> GetTenantIdAsync(string organizationId, CancellationToken ct = default)
            => ValueTask.FromResult<string?>(_tenant);

        public ValueTask<string?> GetOrganizationIdAsync(string tenantId, CancellationToken ct = default)
            => ValueTask.FromResult<string?>(null);

        public ValueTask SetMappingAsync(string organizationId, string tenantId, CancellationToken ct = default)
            => ValueTask.CompletedTask;
    }
}
