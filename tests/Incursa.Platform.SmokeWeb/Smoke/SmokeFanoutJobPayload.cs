using System.Text.Json.Serialization;

namespace Incursa.Platform.SmokeWeb.Smoke;

public sealed record SmokeFanoutJobPayload(
    [property: JsonPropertyName("fanoutTopic")] string FanoutTopic,
    [property: JsonPropertyName("workKey")] string? WorkKey);
