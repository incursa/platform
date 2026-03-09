namespace Incursa.Integrations.Cloudflare.Clients.Models;

public sealed record CloudflareLoadBalancer(
    [property: JsonPropertyName("id")] string? Id,
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("fallback_pool")] string? FallbackPool,
    [property: JsonPropertyName("default_pools")] IReadOnlyList<string>? DefaultPools,
    [property: JsonPropertyName("proxied")] bool? Proxied,
    [property: JsonPropertyName("ttl")] int? Ttl,
    [property: JsonExtensionData] IDictionary<string, JsonElement>? Extra = null);

public sealed record CloudflareLoadBalancerPreviewResult(
    [property: JsonPropertyName("pools")] IDictionary<string, JsonElement>? Pools,
    [property: JsonExtensionData] IDictionary<string, JsonElement>? Extra = null);

public sealed record CloudflareLoadBalancerPool(
    [property: JsonPropertyName("id")] string? Id,
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("monitor")] string? Monitor,
    [property: JsonPropertyName("origins")] IReadOnlyList<CloudflareLoadBalancerOrigin>? Origins,
    [property: JsonExtensionData] IDictionary<string, JsonElement>? Extra = null);

public sealed record CloudflareLoadBalancerOrigin(
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("address")] string? Address,
    [property: JsonPropertyName("enabled")] bool? Enabled,
    [property: JsonPropertyName("weight")] double? Weight,
    [property: JsonPropertyName("header")] IDictionary<string, IReadOnlyList<string>>? Header,
    [property: JsonExtensionData] IDictionary<string, JsonElement>? Extra = null);

public sealed record CloudflarePoolHealthResult(
    [property: JsonPropertyName("pop_health")] IDictionary<string, JsonElement>? PopHealth,
    [property: JsonExtensionData] IDictionary<string, JsonElement>? Extra = null);

public sealed record CloudflareLoadBalancerMonitor(
    [property: JsonPropertyName("id")] string? Id,
    [property: JsonPropertyName("type")] string? Type,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("method")] string? Method,
    [property: JsonPropertyName("path")] string? Path,
    [property: JsonPropertyName("interval")] int? Interval,
    [property: JsonPropertyName("timeout")] int? Timeout,
    [property: JsonPropertyName("retries")] int? Retries,
    [property: JsonExtensionData] IDictionary<string, JsonElement>? Extra = null);

public sealed record CloudflareListResult<T>(
    [property: JsonPropertyName("result")] IReadOnlyList<T>? Result)
{
    public IReadOnlyList<T> Items { get; } = Result ?? Array.Empty<T>();
}

public sealed record CloudflareCreateLoadBalancerRequest(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("fallback_pool")] string FallbackPool,
    [property: JsonPropertyName("default_pools")] IReadOnlyList<string> DefaultPools,
    [property: JsonPropertyName("description")] string? Description = null,
    [property: JsonPropertyName("ttl")] int? Ttl = null,
    [property: JsonPropertyName("proxied")] bool? Proxied = null);

public sealed record CloudflareUpdateLoadBalancerRequest(
    [property: JsonPropertyName("fallback_pool")] string FallbackPool,
    [property: JsonPropertyName("default_pools")] IReadOnlyList<string> DefaultPools,
    [property: JsonPropertyName("description")] string? Description = null,
    [property: JsonPropertyName("ttl")] int? Ttl = null,
    [property: JsonPropertyName("proxied")] bool? Proxied = null);

public sealed record CloudflareCreateLoadBalancerPoolRequest(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("origins")] IReadOnlyList<CloudflareLoadBalancerOrigin> Origins,
    [property: JsonPropertyName("description")] string? Description = null,
    [property: JsonPropertyName("monitor")] string? Monitor = null);

public sealed record CloudflareUpdateLoadBalancerPoolRequest(
    [property: JsonPropertyName("origins")] IReadOnlyList<CloudflareLoadBalancerOrigin> Origins,
    [property: JsonPropertyName("description")] string? Description = null,
    [property: JsonPropertyName("monitor")] string? Monitor = null,
    [property: JsonPropertyName("enabled")] bool? Enabled = null);

public sealed record CloudflareCreateLoadBalancerMonitorRequest(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("method")] string Method,
    [property: JsonPropertyName("path")] string Path,
    [property: JsonPropertyName("description")] string? Description = null,
    [property: JsonPropertyName("interval")] int? Interval = null,
    [property: JsonPropertyName("timeout")] int? Timeout = null,
    [property: JsonPropertyName("retries")] int? Retries = null);

public sealed record CloudflareUpdateLoadBalancerMonitorRequest(
    [property: JsonPropertyName("method")] string Method,
    [property: JsonPropertyName("path")] string Path,
    [property: JsonPropertyName("description")] string? Description = null,
    [property: JsonPropertyName("interval")] int? Interval = null,
    [property: JsonPropertyName("timeout")] int? Timeout = null,
    [property: JsonPropertyName("retries")] int? Retries = null);
