namespace Incursa.Integrations.WorkOS.Tests;

using System.Security.Claims;
using Incursa.Integrations.WorkOS.Abstractions.Authentication;
using Incursa.Integrations.WorkOS.Abstractions.Claims;
using Incursa.Integrations.WorkOS.Abstractions.Configuration;
using Incursa.Integrations.WorkOS.Abstractions.Management;
using Incursa.Integrations.WorkOS.Core.DependencyInjection;
using Incursa.Integrations.WorkOS.Core.Emulation;
using Incursa.Integrations.WorkOS.Core.Clients;
using Incursa.Integrations.WorkOS.Persistence.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

[TestClass]
public sealed class InMemoryWorkOsPlatformTests
{
    [TestMethod]
    public async Task InMemoryPlatform_CreateValidateAndRevokeLifecycleAsync()
    {
        ServiceCollection services = BuildInMemoryPlatformServices();
        using ServiceProvider provider = services.BuildServiceProvider();

        IWorkOsApiKeyManager manager = provider.GetRequiredService<IWorkOsApiKeyManager>();
        IWorkOsApiKeyAuthenticator authenticator = provider.GetRequiredService<IWorkOsApiKeyAuthenticator>();
        IWorkOsManagementAuthorizer authorizer = provider.GetRequiredService<IWorkOsManagementAuthorizer>();
        IWorkOsManagementClient managementClient = provider.GetRequiredService<IWorkOsManagementClient>();

        WorkOsCreatedApiKey created = await manager.CreateAsync("org_1", "Build Agent", ["nuget.read", "raw.push"], ttlHours: 12).ConfigureAwait(false);

        WorkOsApiKeyValidationResult validated = await authenticator.ValidateApiKeyAsync(created.Secret).ConfigureAwait(false);
        Assert.IsTrue(validated.IsValid);
        Assert.IsNotNull(validated.Identity);
        Assert.AreEqual("tenant-1", validated.Identity.TenantId);

        List<WorkOsApiKeySummary> listed = [];
        await foreach (WorkOsApiKeySummary summary in manager.ListAsync("org_1").ConfigureAwait(false))
        {
            listed.Add(summary);
        }

        Assert.HasCount(1, listed);
        Assert.AreEqual(created.ApiKeyId, listed[0].ApiKeyId);

        WorkOsApiKeySummary? fetched = await manager.GetAsync("org_1", created.ApiKeyId).ConfigureAwait(false);
        Assert.IsNotNull(fetched);
        Assert.AreEqual(created.ApiKeyId, fetched.ApiKeyId);

        bool isAdmin = await authorizer.EnsureOrgAdminAsync("org_1", "user_admin").ConfigureAwait(false);
        Assert.IsTrue(isAdmin);

        await manager.RevokeAsync("org_1", created.ApiKeyId).ConfigureAwait(false);
        WorkOsApiKeySummary? revokedSummary = await manager.GetAsync("org_1", created.ApiKeyId).ConfigureAwait(false);
        Assert.IsNotNull(revokedSummary);
        Assert.IsNotNull(revokedSummary.RevokedUtc);

        WorkOsManagedApiKey? managedRevoked = await managementClient.ValidateApiKeyAsync(created.Secret).ConfigureAwait(false);
        Assert.IsNotNull(managedRevoked);
        Assert.IsNotNull(managedRevoked.RevokedUtc);
    }

    [TestMethod]
    public async Task InMemoryPlatform_ClaimsEnrichmentUsesSeededMembershipsAsync()
    {
        ServiceCollection services = BuildInMemoryPlatformServices();
        using ServiceProvider provider = services.BuildServiceProvider();

        IWorkOsClaimsEnricher enricher = provider.GetRequiredService<IWorkOsClaimsEnricher>();
        ClaimsPrincipal principal = new(new ClaimsIdentity([new Claim("sub", "user_member")], "test"));
        ClaimsIdentity identity = (ClaimsIdentity)principal.Identity!;

        await enricher.EnrichAsync(principal, identity).ConfigureAwait(false);

        Assert.IsTrue(identity.HasClaim("org_id", "org_1"));
        Assert.IsTrue(identity.HasClaim("workos:role", "admin"));
        Assert.IsTrue(identity.HasClaim("workos:permission", "conduit:admin"));
    }

    private static ServiceCollection BuildInMemoryPlatformServices()
    {
        ServiceCollection services = new();

        services.AddWorkOsInMemoryPersistence();
        services.AddWorkOsIntegrationCore(
            configureOptions: static options =>
            {
                options.StrictPermissionMapping = false;
            },
            configureManagement: static management =>
            {
                management.BaseUrl = "https://api.workos.test";
                management.ApiKey = "sk_test";
                management.RequestTimeout = TimeSpan.FromSeconds(5);
            });

        services.AddWorkOsInMemoryPlatform(static state =>
        {
            state.SeedTenantMapping("org_1", "tenant-1");
            state.SeedOrganizationAdmin("org_1", "user_admin");
            state.SeedMembership("user_member", new WorkOsOrganizationMembershipInfo("org_1", ["admin"]));
            state.SeedRolePermissions("org_1", "admin", ["conduit:admin"]);
        });

        return services;
    }
}
