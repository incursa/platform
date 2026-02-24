using System.Globalization;
using Incursa.Platform.Health;

namespace Incursa.Platform.HealthProbe;

internal sealed class HealthProbeCommandLine
{
    private HealthProbeCommandLine(
        string? bucketName,
        bool listBuckets,
        TimeSpan? timeoutOverride,
        bool includeData,
        bool jsonOutput)
    {
        BucketName = bucketName;
        ListBuckets = listBuckets;
        TimeoutOverride = timeoutOverride;
        IncludeData = includeData;
        JsonOutput = jsonOutput;
    }

    public string? BucketName { get; }

    public bool ListBuckets { get; }

    public TimeSpan? TimeoutOverride { get; }

    public bool IncludeData { get; }

    public bool JsonOutput { get; }

    public static HealthProbeCommandLine Parse(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        if (args.Length == 0)
        {
            throw new HealthProbeArgumentException($"Missing command. Expected '{HealthProbeDefaults.CommandName}'.");
        }

        if (!HealthProbeApp.IsHealthCheckInvocation(args))
        {
            throw new HealthProbeArgumentException($"Missing '{HealthProbeDefaults.CommandName}' command.");
        }

        string? bucketName = null;
        var listBuckets = false;
        TimeSpan? timeoutOverride = null;
        var includeData = false;
        var jsonOutput = false;

        var index = 1;
        if (index < args.Length && !IsFlag(args[index]))
        {
            if (args[index].Equals("list", StringComparison.OrdinalIgnoreCase))
            {
                listBuckets = true;
            }
            else
            {
                bucketName = args[index];
            }

            index++;
        }

        while (index < args.Length)
        {
            var token = args[index];
            if (!IsFlag(token))
            {
                throw new HealthProbeArgumentException($"Unexpected argument '{token}'.");
            }

            switch (token)
            {
                case "--timeout":
                    timeoutOverride = ParseTimeout(RequireValue(args, ref index, token));
                    break;
                case "--include-data":
                    includeData = true;
                    index++;
                    break;
                case "--json":
                    jsonOutput = true;
                    index++;
                    break;
                default:
                    throw new HealthProbeArgumentException($"Unknown option '{token}'.");
            }
        }

        if (bucketName is not null
            && !bucketName.Equals(PlatformHealthTags.Live, StringComparison.OrdinalIgnoreCase)
            && !bucketName.Equals(PlatformHealthTags.Ready, StringComparison.OrdinalIgnoreCase)
            && !bucketName.Equals(PlatformHealthTags.Dep, StringComparison.OrdinalIgnoreCase))
        {
            throw new HealthProbeArgumentException($"Unknown bucket '{bucketName}'. Expected live, ready, or dep.");
        }

        return new HealthProbeCommandLine(
            bucketName,
            listBuckets,
            timeoutOverride,
            includeData,
            jsonOutput);
    }

    private static bool IsFlag(string value)
    {
        return value.StartsWith("--", StringComparison.Ordinal);
    }

    private static string RequireValue(string[] args, ref int index, string optionName)
    {
        if (index + 1 >= args.Length)
        {
            throw new HealthProbeArgumentException($"Missing value for '{optionName}'.");
        }

        var value = args[index + 1];
        if (IsFlag(value))
        {
            throw new HealthProbeArgumentException($"Missing value for '{optionName}'.");
        }

        index += 2;
        return value;
    }

    private static TimeSpan ParseTimeout(string value)
    {
        if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds))
        {
            throw new HealthProbeArgumentException($"Invalid timeout '{value}'.");
        }

        if (seconds <= 0)
        {
            throw new HealthProbeArgumentException("Timeout must be greater than zero.");
        }

        return TimeSpan.FromSeconds(seconds);
    }
}
