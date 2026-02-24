using System.Globalization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Incursa.Platform.HealthProbe;

internal sealed class HealthProbeOptionsConfigurator : IConfigureOptions<HealthProbeOptions>
{
    private readonly IConfiguration? configuration;

    public HealthProbeOptionsConfigurator(IConfiguration? configuration)
    {
        this.configuration = configuration;
    }

    public void Configure(HealthProbeOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (configuration is null)
        {
            return;
        }

        var section = configuration.GetSection(HealthProbeDefaults.ConfigurationRootKey);
        if (!section.Exists())
        {
            return;
        }

        var defaultBucketValue = section["DefaultBucket"];
        if (!string.IsNullOrWhiteSpace(defaultBucketValue))
        {
            options.DefaultBucket = defaultBucketValue;
        }

        var timeoutValue = section["TimeoutSeconds"];
        if (!string.IsNullOrWhiteSpace(timeoutValue)
            && double.TryParse(timeoutValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds)
            && seconds > 0)
        {
            options.Timeout = TimeSpan.FromSeconds(seconds);
        }

        var includeDataValue = section["IncludeData"];
        if (!string.IsNullOrWhiteSpace(includeDataValue)
            && bool.TryParse(includeDataValue, out var includeData))
        {
            options.IncludeData = includeData;
        }
    }
}
