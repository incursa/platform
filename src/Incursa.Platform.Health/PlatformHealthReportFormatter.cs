using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Incursa.Platform.Health;

public static class PlatformHealthReportFormatter
{
    public static PlatformHealthReportPayload Format(
        PlatformHealthBucket bucket,
        HealthReport report,
        PlatformHealthDataOptions? dataOptions = null)
    {
        ArgumentNullException.ThrowIfNull(report);
        dataOptions ??= new PlatformHealthDataOptions();

        var checks = report.Entries
            .Select(entry => new PlatformHealthCheckEntry(
                entry.Key,
                entry.Value.Status.ToString(),
                entry.Value.Duration.TotalMilliseconds,
                entry.Value.Description,
                ExtractData(entry.Value.Data, dataOptions)))
            .ToArray();

        return new PlatformHealthReportPayload(
            BucketToString(bucket),
            report.Status.ToString(),
            report.TotalDuration.TotalMilliseconds,
            checks);
    }

    public static string BucketToTag(PlatformHealthBucket bucket)
    {
        return bucket switch
        {
            PlatformHealthBucket.Live => PlatformHealthTags.Live,
            PlatformHealthBucket.Ready => PlatformHealthTags.Ready,
            PlatformHealthBucket.Dep => PlatformHealthTags.Dep,
            _ => throw new ArgumentOutOfRangeException(nameof(bucket), bucket, "A single health bucket is required."),
        };
    }

    public static string BucketToString(PlatformHealthBucket bucket)
    {
        return bucket switch
        {
            PlatformHealthBucket.Live => PlatformHealthTags.Live,
            PlatformHealthBucket.Ready => PlatformHealthTags.Ready,
            PlatformHealthBucket.Dep => PlatformHealthTags.Dep,
            _ => throw new ArgumentOutOfRangeException(nameof(bucket), bucket, "A single health bucket is required."),
        };
    }

    private static IReadOnlyDictionary<string, object?>? ExtractData(
        IReadOnlyDictionary<string, object> data,
        PlatformHealthDataOptions options)
    {
        if (!options.IncludeData || data.Count == 0)
        {
            return null;
        }

        var filtered = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var item in data)
        {
            if (IsSensitiveKey(item.Key, options.SensitiveKeyFragments))
            {
                continue;
            }

            filtered[item.Key] = item.Value;
        }

        return filtered.Count == 0 ? null : filtered;
    }

    private static bool IsSensitiveKey(string key, IEnumerable<string> fragments)
    {
        foreach (var fragment in fragments)
        {
            if (key.Contains(fragment, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
