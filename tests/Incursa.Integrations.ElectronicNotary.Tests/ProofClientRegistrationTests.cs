// <copyright file="ProofClientRegistrationTests.cs" company="Incursa">
// CONFIDENTIAL - Copyright (c) Incursa. All rights reserved.
// See NOTICE.md for full restrictions and usage terms.
// </copyright>

namespace Incursa.Integrations.ElectronicNotary.Tests;

using System.Net;
using FluentAssertions;
using Incursa.Integrations.ElectronicNotary.Proof;
using Incursa.Integrations.ElectronicNotary.Proof.Types;
using Microsoft.Extensions.DependencyInjection;

[TestClass]
public sealed class ProofClientRegistrationTests
{
    [TestMethod]
    public async Task AddProofClientUsesFairfaxAndApiKeyHeaderAsync()
    {
        var services = new ServiceCollection();
        using var handler = new CapturingHttpMessageHandler();
        services.AddSingleton(handler);
        services.AddProofClient(options =>
        {
            options.Environment = ProofEnvironment.Fairfax;
            options.ApiKey = "test-api-key";
        });
        services.AddHttpClient<IProofClient, ProofClient>()
            .ConfigurePrimaryHttpMessageHandler(static serviceProvider => serviceProvider.GetRequiredService<CapturingHttpMessageHandler>());

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        IProofClient client = serviceProvider.GetRequiredService<IProofClient>();
        var transaction = await client.GetTransactionAsync(ProofTransactionId.Parse("ot_wd3y67d"), CancellationToken.None).ConfigureAwait(false);
        transaction.Id.Should().Be(ProofTransactionId.Parse("ot_wd3y67d"));

        handler.LastRequest.Should().NotBeNull();
        handler.LastRequest!.RequestUri.Should().Be(new Uri("https://api.fairfax.proof.com/transactions/ot_wd3y67d"));
        handler.LastRequest.Headers.TryGetValues("ApiKey", out IEnumerable<string>? values).Should().BeTrue();
        values.Should().NotBeNull();
        values!.Should().ContainSingle().Which.Should().Be("test-api-key");
    }

    [TestMethod]
    public async Task AddProofClientPrefersExplicitBaseUrlAsync()
    {
        var services = new ServiceCollection();
        using var handler = new CapturingHttpMessageHandler();
        services.AddSingleton(handler);
        services.AddProofClient(options =>
        {
            options.Environment = ProofEnvironment.Production;
            options.BaseUrl = new Uri("https://example.test/custom");
            options.ApiKey = "test-api-key";
        });
        services.AddHttpClient<IProofClient, ProofClient>()
            .ConfigurePrimaryHttpMessageHandler(static serviceProvider => serviceProvider.GetRequiredService<CapturingHttpMessageHandler>());

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        IProofClient client = serviceProvider.GetRequiredService<IProofClient>();
        var transaction = await client.GetTransactionAsync(ProofTransactionId.Parse("ot_wd3y67d"), CancellationToken.None).ConfigureAwait(false);
        transaction.Id.Should().Be(ProofTransactionId.Parse("ot_wd3y67d"));

        handler.LastRequest.Should().NotBeNull();
        handler.LastRequest!.RequestUri.Should().Be(new Uri("https://example.test/custom/transactions/ot_wd3y67d"));
    }

    [TestMethod]
    public void AddProofClientThrowsWhenApiKeyMissing()
    {
        var services = new ServiceCollection();
        using var handler = new CapturingHttpMessageHandler();
        services.AddSingleton(handler);
        services.AddProofClient(options =>
        {
            options.Environment = ProofEnvironment.Production;
            options.ApiKey = string.Empty;
        });
        services.AddHttpClient<IProofClient, ProofClient>()
            .ConfigurePrimaryHttpMessageHandler(static serviceProvider => serviceProvider.GetRequiredService<CapturingHttpMessageHandler>());

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        Action act = () => _ = serviceProvider.GetRequiredService<IProofClient>();

        InvalidOperationException exception = act.Should().Throw<InvalidOperationException>().Which;
        exception.Message.Should().Contain("ApiKey");
    }

    [TestMethod]
    public void AddProofClientThrowsWhenTimeoutNotPositive()
    {
        var services = new ServiceCollection();
        using var handler = new CapturingHttpMessageHandler();
        services.AddSingleton(handler);
        services.AddProofClient(options =>
        {
            options.Environment = ProofEnvironment.Production;
            options.ApiKey = "test-api-key";
            options.Timeout = TimeSpan.Zero;
        });
        services.AddHttpClient<IProofClient, ProofClient>()
            .ConfigurePrimaryHttpMessageHandler(static serviceProvider => serviceProvider.GetRequiredService<CapturingHttpMessageHandler>());

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        Action act = () => _ = serviceProvider.GetRequiredService<IProofClient>();

        InvalidOperationException exception = act.Should().Throw<InvalidOperationException>().Which;
        exception.Message.Should().Contain("timeout");
    }

    [TestMethod]
    public void AddProofClientThrowsWhenBaseUrlIsRelative()
    {
        var services = new ServiceCollection();
        using var handler = new CapturingHttpMessageHandler();
        services.AddSingleton(handler);
        services.AddProofClient(options =>
        {
            options.Environment = ProofEnvironment.Production;
            options.BaseUrl = new Uri("/relative", UriKind.Relative);
            options.ApiKey = "test-api-key";
        });
        services.AddHttpClient<IProofClient, ProofClient>()
            .ConfigurePrimaryHttpMessageHandler(static serviceProvider => serviceProvider.GetRequiredService<CapturingHttpMessageHandler>());

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        Action act = () => _ = serviceProvider.GetRequiredService<IProofClient>();

        InvalidOperationException exception = act.Should().Throw<InvalidOperationException>().Which;
        exception.Message.Should().Contain("absolute URI");
    }

    private sealed class CapturingHttpMessageHandler : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            this.LastRequest = request;
            return Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("""{"id":"ot_wd3y67d"}"""),
                });
        }
    }
}
