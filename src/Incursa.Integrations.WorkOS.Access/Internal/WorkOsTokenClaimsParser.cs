namespace Incursa.Integrations.WorkOS.Access;

using System.Globalization;
using System.Text.Json;
using Incursa.Integrations.WorkOS.Abstractions.Authentication;

internal static class WorkOsTokenClaimsParser
{
    private static readonly string[] SessionIdCandidates = ["sid", "session_id", "workos_session_id"];
    private static readonly string[] OrganizationIdCandidates = ["org_id", "organization_id", "workos:org_id", "workos:organization_id"];
    private static readonly string[] RoleCandidates = ["role", "roles", "workos:role", "workos:roles"];
    private static readonly string[] PermissionCandidates = ["permission", "permissions", "workos:permission", "workos:permissions"];
    private static readonly string[] FeatureFlagCandidates = ["feature_flag", "feature_flags", "featureFlags", "workos:feature_flag", "workos:feature_flags"];
    private static readonly string[] EntitlementCandidates = ["entitlement", "entitlements", "workos:entitlement", "workos:entitlements"];

    public static bool TryParse(string? accessToken, out WorkOsTokenClaims? claims)
    {
        claims = null;
        if (string.IsNullOrWhiteSpace(accessToken) || !TryReadPayload(accessToken, out var payload))
        {
            return false;
        }

        var subject = ReadFirst(payload, "sub", "user_id", "workos:user_id");
        if (string.IsNullOrWhiteSpace(subject))
        {
            return false;
        }

        claims = new WorkOsTokenClaims(
            subject,
            ReadFirst(payload, SessionIdCandidates),
            ReadFirst(payload, OrganizationIdCandidates),
            ReadSet(payload, RoleCandidates),
            ReadSet(payload, PermissionCandidates),
            ReadSet(payload, FeatureFlagCandidates),
            ReadSet(payload, EntitlementCandidates),
            ReadExpiration(payload));

        return true;
    }

    private static bool TryReadPayload(string token, out JsonElement payload)
    {
        payload = default;
        var parts = token.Split('.');
        if (parts.Length < 2)
        {
            return false;
        }

        try
        {
            var bytes = DecodeBase64Url(parts[1]);
            using var document = JsonDocument.Parse(bytes);
            payload = document.RootElement.Clone();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string? ReadFirst(JsonElement payload, params string[] candidates)
    {
        foreach (var candidate in candidates)
        {
            if (!payload.TryGetProperty(candidate, out var value))
            {
                continue;
            }

            if (value.ValueKind == JsonValueKind.String)
            {
                var stringValue = value.GetString();
                if (!string.IsNullOrWhiteSpace(stringValue))
                {
                    return stringValue.Trim();
                }
            }
        }

        return null;
    }

    private static IReadOnlyCollection<string> ReadSet(JsonElement payload, IEnumerable<string> candidates)
    {
        HashSet<string> values = new(StringComparer.OrdinalIgnoreCase);
        foreach (var candidate in candidates)
        {
            if (!payload.TryGetProperty(candidate, out var value))
            {
                continue;
            }

            Add(values, value);
        }

        return values.OrderBy(static value => value, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static void Add(HashSet<string> values, JsonElement value)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.Array:
                foreach (var item in value.EnumerateArray())
                {
                    Add(values, item);
                }

                return;

            case JsonValueKind.String:
                Add(values, value.GetString());
                return;
        }
    }

    private static void Add(HashSet<string> values, string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return;
        }

        var trimmed = raw.Trim();
        if (trimmed.StartsWith("[", StringComparison.Ordinal))
        {
            try
            {
                using var document = JsonDocument.Parse(trimmed);
                if (document.RootElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in document.RootElement.EnumerateArray())
                    {
                        Add(values, item);
                    }

                    return;
                }
            }
            catch (JsonException)
            {
                // Fall back to tokenized parsing.
            }
        }

        foreach (var part in trimmed.Split(new[] { ',', ';', ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries))
        {
            values.Add(part.Trim());
        }
    }

    private static DateTimeOffset? ReadExpiration(JsonElement payload)
    {
        if (!payload.TryGetProperty("exp", out var exp))
        {
            return null;
        }

        if (exp.ValueKind == JsonValueKind.Number && exp.TryGetInt64(out var unixSeconds))
        {
            return DateTimeOffset.FromUnixTimeSeconds(unixSeconds);
        }

        return exp.ValueKind == JsonValueKind.String && long.TryParse(exp.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out unixSeconds)
            ? DateTimeOffset.FromUnixTimeSeconds(unixSeconds)
            : null;
    }

    private static byte[] DecodeBase64Url(string input)
    {
        var base64 = input.Replace('-', '+').Replace('_', '/');
        var pad = base64.Length % 4;
        if (pad > 0)
        {
            base64 += new string('=', 4 - pad);
        }

        return Convert.FromBase64String(base64);
    }
}
