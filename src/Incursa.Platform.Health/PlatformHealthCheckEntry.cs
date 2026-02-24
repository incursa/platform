namespace Incursa.Platform.Health;

public sealed record PlatformHealthCheckEntry(
    string Name,
    string Status,
    double DurationMs,
    string? Description,
    IReadOnlyDictionary<string, object?>? Data);
