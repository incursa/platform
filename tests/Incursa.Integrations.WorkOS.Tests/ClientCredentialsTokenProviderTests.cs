namespace Incursa.Integrations.WorkOS.Tests;

using System.Net;
using Incursa.Integrations.WorkOS.Abstractions.Configuration;
using Incursa.Integrations.WorkOS.Core.Claims;

[TestClass]
public sealed class ClientCredentialsTokenProviderTests
{
    [TestMethod]
    public async Task GetAccessTokenAsync_CachesTokenUntilRefreshWindow()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"access_token\":\"tok_1\",\"expires_in\":3600}", Encoding.UTF8, "application/json"),
        });

        var httpClient = new HttpClient(handler);
        var provider = new WorkOsClientCredentialsTokenProvider(httpClient, new WorkOsClientCredentialsOptions
        {
            Authority = "https://tenant.authkit.app",
            ClientId = "client_1",
            ClientSecret = "secret_1",
            Scope = "conduit:resolve",
        });

        var t1 = await provider.GetAccessTokenAsync();
        var t2 = await provider.GetAccessTokenAsync();

        Assert.AreEqual("tok_1", t1);
        Assert.AreEqual("tok_1", t2);
        Assert.AreEqual(1, handler.RequestCount);
    }

    [TestMethod]
    public async Task GetAccessTokenAsync_Failure_Throws()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("{\"error\":\"invalid_client\"}", Encoding.UTF8, "application/json"),
        });

        var httpClient = new HttpClient(handler);
        var provider = new WorkOsClientCredentialsTokenProvider(httpClient, new WorkOsClientCredentialsOptions
        {
            Authority = "https://tenant.authkit.app",
            ClientId = "client_1",
            ClientSecret = "secret_1",
            Scope = "conduit:resolve",
            RetryCount = 1,
        });

        var threw = false;
        try
        {
            _ = await provider.GetAccessTokenAsync();
        }
        catch (InvalidOperationException)
        {
            threw = true;
        }

        Assert.IsTrue(threw);
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handler = handler;
        }

        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;
            return Task.FromResult(_handler(request));
        }
    }
}
