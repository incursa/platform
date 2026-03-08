using Microsoft.Extensions.Configuration;

namespace Incursa.Platform.HealthProbe.Tests;

public sealed class HealthProbeOptionsConfiguratorTests
{
    [Fact]
    public void Configure_BindsModeAndHttpOptions()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["Incursa:HealthProbe:Mode"] = "http",
                ["Incursa:HealthProbe:Http:BaseUrl"] = "https://example.local",
                ["Incursa:HealthProbe:Http:LivePath"] = "/livez",
                ["Incursa:HealthProbe:Http:ReadyPath"] = "/readyz",
                ["Incursa:HealthProbe:Http:DepPath"] = "/depz",
                ["Incursa:HealthProbe:Http:ApiKey"] = "abc123",
                ["Incursa:HealthProbe:Http:ApiKeyHeaderName"] = "X-App-Key",
                ["Incursa:HealthProbe:Http:AllowInsecureTls"] = "true",
            })
            .Build();

        var options = new HealthProbeOptions();
        var configurator = new HealthProbeOptionsConfigurator(config);

        configurator.Configure(options);

        options.Mode.ShouldBe(HealthProbeMode.Http);
        options.Http.BaseUrl.ShouldBe(new Uri("https://example.local"));
        options.Http.LivePath.ShouldBe("/livez");
        options.Http.ReadyPath.ShouldBe("/readyz");
        options.Http.DepPath.ShouldBe("/depz");
        options.Http.ApiKey.ShouldBe("abc123");
        options.Http.ApiKeyHeaderName.ShouldBe("X-App-Key");
        options.Http.AllowInsecureTls.ShouldBeTrue();
    }

    [Fact]
    public void Configure_ThrowsForUnknownMode()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["Incursa:HealthProbe:Mode"] = "grpc",
            })
            .Build();

        var options = new HealthProbeOptions();
        var configurator = new HealthProbeOptionsConfigurator(config);

        var exception = Should.Throw<HealthProbeArgumentException>(() => configurator.Configure(options));

        exception.Message.ShouldContain("Unknown mode");
    }
}
