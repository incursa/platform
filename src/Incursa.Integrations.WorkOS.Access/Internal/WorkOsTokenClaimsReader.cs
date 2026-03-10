namespace Incursa.Integrations.WorkOS.Access;

using System.Text.Json;
using Incursa.Integrations.WorkOS.Abstractions.Authentication;

internal static class WorkOsTokenClaimsReader
{
    private static readonly string[] SubjectClaimCandidates = ["sub", "workos:user_id"];
    private static readonly string[] SessionClaimCandidates = ["sid", "session_id", "workos_session_id"];
    private static readonly string[] OrganizationClaimCandidates = ["org_id", "organization_id", "workos:org"];
    private static readonly string[] RoleClaimCandidates = ["workos:role", "roles", "role"];
    private static readonly string[] PermissionClaimCandidates = ["workos:permission", "permissions", "permission"];
    private static readonly string[] FeatureFlagClaimCandidates = ["feature_flag", "feature_flags", "featureFlags"];
    private static readonly string[] EntitlementClaimCandidates = ["entitlement", "entitlements"];

    public static bool TryRead(string token, out WorkOsTokenClaims? accessContext)
    {
        accessContext = null;
        if (!TryReadPayload(token, out var payload))
        {
            return false;
        }

        var subjectId = ReadFirstString(payload, SubjectClaimCandidates);
        if (string.IsNullOrWhiteSpace(subjectId))
        {
            return false;
        }

        accessContext = new WorkOsTokenClaims(
            subjectId,
            ReadFirstString(payload, SessionClaimCandidates),
            ReadFirstString(payload, OrganizationClaimCandidates),
            ReadStringSet(payload, RoleClaimCandidates),
            ReadStringSet(payload, PermissionClaimCandidates),
            ReadStringSet(payload, FeatureFlagClaimCandidates),
            ReadStringSet(payload, EntitlementClaimCandidates),
            ReadUnixExpiry(payload));
        return true;
    }

    private static bool TryReadPayload(string token, out JsonElement payload)
    {
        payload = default;
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

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

    private static string? ReadFirstString(JsonElement payload, IEnumerable<string> claimNames)
    {
        foreach (var claimName in claimNames)
        {
            if (payload.TryGetProperty(claimName, out var value) && value.ValueKind == JsonValueKind.String)
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

    private static IReadOnlyCollection<string> ReadStringSet(JsonElement payload, IEnumerable<string> claimNames)
    {
        HashSet<string> values = new(StringComparer.OrdinalIgnoreCase);

        foreach (var claimName in claimNames)
        {
            if (!payload.TryGetProperty(claimName, out var value))
            {
                continue;
            }

            switch (value.ValueKind)
            {
                case JsonValueKind.Array:
                    foreach (var item in value.EnumerateArray())
                    {
                        if (item.ValueKind == JsonValueKind.String)
                        {
                            Add(values, item.GetString());
                        }
                    }

                    break;

                case JsonValueKind.String:
                    Add(values, value.GetString());
                    break;
            }
        }

        return values.OrderBy(static item => item, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static DateTimeOffset? ReadUnixExpiry(JsonElement payload)
    {
        if (!payload.TryGetProperty("exp", out var expValue))
        {
            return null;
        }

        return expValue.ValueKind switch
        {
            JsonValueKind.Number when expValue.TryGetInt64(out var unixSeconds) => DateTimeOffset.FromUnixTimeSeconds(unixSeconds),
            _ => null,
        };
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
                        if (item.ValueKind == JsonValueKind.String)
                        {
                            var value = item.GetString();
                            if (!string.IsNullOrWhiteSpace(value))
                            {
                                values.Add(value.Trim());
                            }
                        }
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

    private static byte[] DecodeBase64Url(string input)
    {
        var normalized = input.Replace('-', '+').Replace('_', '/');
        var padding = normalized.Length % 4;
        if (padding > 0)
        {
            normalized += new string('=', 4 - padding);
        }

        return Convert.FromBase64String(normalized);
    }
}
