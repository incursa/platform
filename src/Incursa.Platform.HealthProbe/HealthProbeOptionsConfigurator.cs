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

        var modeValue = section["Mode"];
        if (!string.IsNullOrWhiteSpace(modeValue))
        {
            if (!Enum.TryParse<HealthProbeMode>(modeValue, true, out var mode))
            {
                throw new HealthProbeArgumentException($"Unknown mode '{modeValue}'. Expected inprocess or http.");
            }

            options.Mode = mode;
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

        ConfigureHttpOptions(section.GetSection("Http"), options.Http);
    }

    private static void ConfigureHttpOptions(IConfigurationSection section, HealthProbeHttpOptions http)
    {
        ArgumentNullException.ThrowIfNull(section);
        ArgumentNullException.ThrowIfNull(http);

        if (!section.Exists())
        {
            return;
        }

        var baseUrl = section["BaseUrl"];
        if (!string.IsNullOrWhiteSpace(baseUrl))
        {
            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var parsed))
            {
                throw new HealthProbeArgumentException($"Invalid HTTP base URL '{baseUrl}'.");
            }

            http.BaseUrl = parsed;
        }

        var livePath = section["LivePath"];
        if (!string.IsNullOrWhiteSpace(livePath))
        {
            http.LivePath = livePath;
        }

        var readyPath = section["ReadyPath"];
        if (!string.IsNullOrWhiteSpace(readyPath))
        {
            http.ReadyPath = readyPath;
        }

        var depPath = section["DepPath"];
        if (!string.IsNullOrWhiteSpace(depPath))
        {
            http.DepPath = depPath;
        }

        var apiKey = section["ApiKey"];
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            http.ApiKey = apiKey;
        }

        var apiKeyHeaderName = section["ApiKeyHeaderName"];
        if (!string.IsNullOrWhiteSpace(apiKeyHeaderName))
        {
            http.ApiKeyHeaderName = apiKeyHeaderName;
        }

        var allowInsecureTls = section["AllowInsecureTls"];
        if (!string.IsNullOrWhiteSpace(allowInsecureTls)
            && bool.TryParse(allowInsecureTls, out var parsedAllowInsecureTls))
        {
            http.AllowInsecureTls = parsedAllowInsecureTls;
        }
    }
}
