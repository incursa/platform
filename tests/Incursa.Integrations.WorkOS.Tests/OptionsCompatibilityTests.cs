namespace Incursa.Integrations.WorkOS.Tests;

using Incursa.Integrations.WorkOS.Abstractions.Configuration;
using Incursa.Integrations.WorkOS.Core.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

[TestClass]
public sealed class OptionsCompatibilityTests
{
    [TestMethod]
    public void AddWorkOsIntegrationCore_LegacyOptions_MapsToManagementOptions()
    {
        var services = new ServiceCollection();
        services.AddWorkOsIntegrationCore(
            configureOptions: o =>
            {
                o.BaseUrl = "https://api.workos.test";
                o.ApiKey = "sk_test_123";
                o.RequestTimeout = TimeSpan.FromSeconds(9);
                o.WebhookSigningSecret = "whsec";
            });

        using var provider = services.BuildServiceProvider();
        var management = provider.GetRequiredService<WorkOsManagementOptions>();

        Assert.AreEqual("https://api.workos.test", management.BaseUrl);
        Assert.AreEqual("sk_test_123", management.ApiKey);
        Assert.AreEqual(TimeSpan.FromSeconds(9), management.RequestTimeout);
    }
}
