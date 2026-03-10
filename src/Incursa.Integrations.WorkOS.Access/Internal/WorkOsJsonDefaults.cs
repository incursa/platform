namespace Incursa.Integrations.WorkOS.Access;

using System.Text.Json;
using System.Text.Json.Serialization;

internal static class WorkOsJsonDefaults
{
    public static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };
}
