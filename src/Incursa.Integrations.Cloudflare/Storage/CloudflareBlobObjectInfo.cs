namespace Incursa.Integrations.Cloudflare.Storage;

public sealed record CloudflareBlobObjectInfo(
    string Key,
    long Size,
    string? ContentType,
    DateTimeOffset? LastModifiedUtc);
