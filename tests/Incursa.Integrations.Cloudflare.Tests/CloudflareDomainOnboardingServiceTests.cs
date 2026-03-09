using Incursa.Integrations.Cloudflare.Clients;
using Incursa.Integrations.Cloudflare.Clients.Models;
using Incursa.Integrations.Cloudflare.Services;

namespace Incursa.Integrations.Cloudflare.Tests;

public sealed class CloudflareDomainOnboardingServiceTests
{
    [Fact]
    public async Task CreateOrFetchCustomHostnameAsync_ReturnsExisting_WhenFoundAsync()
    {
        FakeCustomHostnameClient client = new FakeCustomHostnameClient
        {
            ExistingByHostname = new CloudflareCustomHostname(
                "h1",
                "tenant.example.com",
                "active",
                new CloudflareCustomHostnameSsl("active", "http", null),
                new CloudflareOwnershipVerification("_cf-custom-hostname", "txt", "abc")),
        };

        CloudflareDomainOnboardingService sut = new CloudflareDomainOnboardingService(client);
        var result = await sut.CreateOrFetchCustomHostnameAsync("tenant.example.com", CancellationToken.None);

        Assert.Equal("h1", result.Id);
        Assert.Equal("active", result.Status);
        Assert.Equal(0, client.CreateCalls);
    }

    [Fact]
    public async Task CreateOrFetchCustomHostnameAsync_Creates_WhenMissingAsync()
    {
        FakeCustomHostnameClient client = new FakeCustomHostnameClient
        {
            Created = new CloudflareCustomHostname(
                "h2",
                "new.example.com",
                "pending",
                new CloudflareCustomHostnameSsl("pending_validation", "http", null),
                new CloudflareOwnershipVerification("_cf-custom-hostname", "txt", "xyz")),
        };

        CloudflareDomainOnboardingService sut = new CloudflareDomainOnboardingService(client);
        var result = await sut.CreateOrFetchCustomHostnameAsync("new.example.com", CancellationToken.None);

        Assert.Equal("h2", result.Id);
        Assert.Equal("new.example.com", client.LastCreateRequest?.Hostname);
        Assert.Equal(1, client.CreateCalls);
    }

    private sealed class FakeCustomHostnameClient : ICloudflareCustomHostnameClient
    {
        public CloudflareCustomHostname? ExistingByHostname { get; init; }

        public CloudflareCustomHostname? Created { get; init; }

        public CloudflareCreateCustomHostnameRequest? LastCreateRequest { get; private set; }

        public int CreateCalls { get; private set; }

        public Task<CloudflareCustomHostname> CreateAsync(CloudflareCreateCustomHostnameRequest request, CancellationToken cancellationToken = default)
        {
            CreateCalls++;
            LastCreateRequest = request;
            return Task.FromResult(Created ?? throw new InvalidOperationException("Created payload not configured."));
        }

        public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<CloudflareCustomHostname?> GetByHostnameAsync(string hostname, CancellationToken cancellationToken = default)
            => Task.FromResult(ExistingByHostname);

        public Task<CloudflareCustomHostname?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
            => Task.FromResult<CloudflareCustomHostname?>(null);

        public Task<CloudflareCustomHostname> PatchAsync(string id, CloudflarePatchCustomHostnameRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(Created ?? throw new InvalidOperationException("Created payload not configured."));
    }
}
