namespace Incursa.Integrations.Cloudflare.Tests.TestInfrastructure;

internal sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responseFactory;

    public StubHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responseFactory)
    {
        this.responseFactory = responseFactory ?? throw new ArgumentNullException(nameof(responseFactory));
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => responseFactory(request, cancellationToken);
}
