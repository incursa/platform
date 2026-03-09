namespace Incursa.Integrations.ElectronicNotary.Tests;

using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using Bravellian.Platform;
using Bravellian.Platform.Webhooks;
using FluentAssertions;
using Incursa.Integrations.ElectronicNotary.Proof.AspNetCore;
using Incursa.Integrations.ElectronicNotary.Proof.Contracts;
using Incursa.Integrations.ElectronicNotary.Proof.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

[TestClass]
public sealed class ProofWebhookAspNetCoreIntegrationTests
{
    private const string SigningKey = "proof-integration-signing-key";

    [TestMethod]
    public async Task SignedWebhookIsAcceptedThenProcessedAndDedupedAsync()
    {
        var recordingHandler = new RecordingProofWebhookHandler();
        WebApplication app = await BuildAppAsync(recordingHandler).ConfigureAwait(false);
        try
        {
            HttpClient client = app.GetTestClient();
            IWebhookProcessor processor = app.Services.GetRequiredService<IWebhookProcessor>();
            const string Body = "{\"event\":\"transaction.completed\",\"data\":{\"transaction_id\":\"ot_wd3y67d\",\"date_occurred\":\"2026-02-06T00:00:00Z\"}}";

            HttpResponseMessage acceptedResponse = await PostWebhookAsync(client, Body).ConfigureAwait(false);
            acceptedResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            int firstRunProcessed = await processor.RunOnceAsync(CancellationToken.None).ConfigureAwait(false);
            firstRunProcessed.Should().Be(1);
            recordingHandler.CompletedEvents.Should().ContainSingle();
            recordingHandler.CompletedEvents[0].TransactionId.Should().Be(new ProofTransactionId("ot_wd3y67d"));

            HttpResponseMessage duplicateResponse = await PostWebhookAsync(client, Body).ConfigureAwait(false);
            duplicateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            await processor.RunOnceAsync(CancellationToken.None).ConfigureAwait(false);
            recordingHandler.CompletedEvents.Should().ContainSingle();
        }
        finally
        {
            await app.DisposeAsync().ConfigureAwait(false);
        }
    }

    [TestMethod]
    public async Task InvalidSignatureIsRejectedAndNotProcessedAsync()
    {
        var recordingHandler = new RecordingProofWebhookHandler();
        WebApplication app = await BuildAppAsync(recordingHandler).ConfigureAwait(false);
        try
        {
            HttpClient client = app.GetTestClient();
            IWebhookProcessor processor = app.Services.GetRequiredService<IWebhookProcessor>();
            const string Body = "{\"event\":\"transaction.completed\",\"data\":{\"transaction_id\":\"ot_wd3y67d\",\"date_occurred\":\"2026-02-06T00:00:00Z\"}}";

            HttpResponseMessage response = await PostWebhookAsync(client, Body, "bad-signature").ConfigureAwait(false);
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

            int processed = await processor.RunOnceAsync(CancellationToken.None).ConfigureAwait(false);
            processed.Should().Be(0);
            recordingHandler.CompletedEvents.Should().BeEmpty();
        }
        finally
        {
            await app.DisposeAsync().ConfigureAwait(false);
        }
    }

    private static async Task<WebApplication> BuildAppAsync(RecordingProofWebhookHandler recordingHandler)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddInMemoryPlatformMultiDatabaseWithList(new[]
        {
            new InMemoryPlatformDatabase { Name = "default" },
        });
        builder.Services.AddProofWebhooks(options =>
        {
            options.SigningKey = SigningKey;
            options.RequireSignature = true;
        });
        builder.Services.RemoveAll<IHostedService>();
        builder.Services.AddSingleton<IProofWebhookHandler>(recordingHandler);

        WebApplication app = builder.Build();
        app.MapProofWebhooks();
        await app.StartAsync(CancellationToken.None).ConfigureAwait(false);
        return app;
    }

    private static async Task<HttpResponseMessage> PostWebhookAsync(HttpClient client, string body, string? signature = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/webhooks/proof");
        request.Content = new StringContent(body, Encoding.UTF8, "application/json");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Add(ProofWebhookOptions.SignatureHeaderName, signature ?? CreateSignature(body, SigningKey));
        return await client.SendAsync(request, CancellationToken.None).ConfigureAwait(false);
    }

    private static string CreateSignature(string body, string signingKey)
    {
        byte[] keyBytes = Encoding.UTF8.GetBytes(signingKey);
        using var hmac = new HMACSHA256(keyBytes);
        byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(body));
        return Convert.ToHexString(hash).ToUpperInvariant();
    }

    private sealed class RecordingProofWebhookHandler : IProofWebhookHandler
    {
        public List<TransactionCompletedEvent> CompletedEvents { get; } = new List<TransactionCompletedEvent>();

        public Task OnTransactionCompletedAsync(TransactionCompletedEvent evt)
        {
            this.CompletedEvents.Add(evt);
            return Task.CompletedTask;
        }

        public Task OnTransactionReleasedAsync(TransactionReleasedEvent evt)
        {
            return Task.CompletedTask;
        }

        public Task OnTransactionCompletedWithRejectionsAsync(TransactionCompletedWithRejectionsEvent evt)
        {
            return Task.CompletedTask;
        }

        public Task OnUnknownAsync(ProofWebhookEnvelope envelope)
        {
            return Task.CompletedTask;
        }
    }
}
