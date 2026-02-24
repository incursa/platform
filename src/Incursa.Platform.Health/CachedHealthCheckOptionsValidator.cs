using Microsoft.Extensions.Options;

namespace Incursa.Platform.Health;

public sealed class CachedHealthCheckOptionsValidator : IValidateOptions<CachedHealthCheckOptions>
{
    public ValidateOptionsResult Validate(string? name, CachedHealthCheckOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.HealthyCacheDuration < TimeSpan.Zero)
        {
            return ValidateOptionsResult.Fail($"{nameof(CachedHealthCheckOptions.HealthyCacheDuration)} must be non-negative.");
        }

        if (options.DegradedCacheDuration < TimeSpan.Zero)
        {
            return ValidateOptionsResult.Fail($"{nameof(CachedHealthCheckOptions.DegradedCacheDuration)} must be non-negative.");
        }

        if (options.UnhealthyCacheDuration < TimeSpan.Zero)
        {
            return ValidateOptionsResult.Fail($"{nameof(CachedHealthCheckOptions.UnhealthyCacheDuration)} must be non-negative.");
        }

        return ValidateOptionsResult.Success;
    }
}
