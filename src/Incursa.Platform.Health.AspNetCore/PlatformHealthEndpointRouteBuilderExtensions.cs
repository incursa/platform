using Incursa.Platform.Health;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Incursa.Platform.Health.AspNetCore;

public static class PlatformHealthEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapPlatformHealthEndpoints(
        this IEndpointRouteBuilder endpoints,
        Action<PlatformHealthEndpointOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var options = new PlatformHealthEndpointOptions();
        configure?.Invoke(options);

        MapBucketEndpoint(endpoints, PlatformHealthBucket.Live, PlatformHealthEndpoints.Live, options);
        MapBucketEndpoint(endpoints, PlatformHealthBucket.Ready, PlatformHealthEndpoints.Ready, options);
        MapBucketEndpoint(endpoints, PlatformHealthBucket.Dep, PlatformHealthEndpoints.Dep, options);

        return endpoints;
    }

    private static void MapBucketEndpoint(
        IEndpointRouteBuilder endpoints,
        PlatformHealthBucket bucket,
        string pattern,
        PlatformHealthEndpointOptions options)
    {
        var tag = PlatformHealthReportFormatter.BucketToTag(bucket);

        var endpointConventionBuilder = endpoints.MapHealthChecks(pattern, new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains(tag, StringComparer.Ordinal),
            ResultStatusCodes =
            {
                [HealthStatus.Healthy] = StatusCodes.Status200OK,
                [HealthStatus.Degraded] = StatusCodes.Status503ServiceUnavailable,
                [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable,
            },
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json; charset=utf-8";
                var payload = PlatformHealthReportFormatter.Format(
                    bucket,
                    report,
                    new PlatformHealthDataOptions
                    {
                        IncludeData = options.IncludeData,
                    });
                await context.Response.WriteAsJsonAsync(payload).ConfigureAwait(false);
            },
        });

        if (!options.RequireAuthorization)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(options.AuthorizationPolicy))
        {
            endpointConventionBuilder.RequireAuthorization();
            return;
        }

        endpointConventionBuilder.RequireAuthorization(options.AuthorizationPolicy);
    }
}
