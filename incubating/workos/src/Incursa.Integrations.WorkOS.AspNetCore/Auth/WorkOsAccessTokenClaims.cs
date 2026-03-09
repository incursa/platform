namespace Incursa.Integrations.WorkOS.AspNetCore.Auth;

using System.Security.Claims;

internal static class WorkOsAccessTokenClaims
{
    private static readonly string[] OrgClaimCandidates = ["org_id", "organization_id", "workos:org"];
    private static readonly string[] RoleClaimCandidates = ["workos:role", "roles", "role"];
    private static readonly string[] PermissionClaimCandidates = ["workos:permission", "permissions", "permission"];
    private static readonly string[] SessionClaimCandidates = ["sid", "session_id", "workos_session_id"];
    private static readonly string[] SessionClaimTargets = ["sid", "session_id", "workos_session_id"];

    public static void TryAddClaimsFromAccessToken(string accessToken, ClaimsIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(identity);
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return;
        }

        if (!TryReadPayload(accessToken, out var payload))
        {
            return;
        }

        foreach (var candidate in OrgClaimCandidates)
        {
            if (payload.TryGetProperty(candidate, out var orgValue) && orgValue.ValueKind == JsonValueKind.String)
            {
                AddIfMissing(identity, "org_id", orgValue.GetString());
            }
        }

        AddArrayOrStringClaims(identity, payload, RoleClaimCandidates, "workos:role");
        AddArrayOrStringClaims(identity, payload, PermissionClaimCandidates, "workos:permission");
        AddSessionClaims(identity, payload);
    }

    private static void AddSessionClaims(ClaimsIdentity identity, JsonElement payload)
    {
        foreach (var candidate in SessionClaimCandidates)
        {
            if (!payload.TryGetProperty(candidate, out var sessionValue) || sessionValue.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            var value = sessionValue.GetString();
            foreach (var target in SessionClaimTargets)
            {
                AddIfMissing(identity, target, value);
            }

            return;
        }
    }

    private static void AddArrayOrStringClaims(ClaimsIdentity identity, JsonElement payload, IEnumerable<string> claimCandidates, string targetClaimType)
    {
        foreach (var candidate in claimCandidates)
        {
            if (!payload.TryGetProperty(candidate, out var value))
            {
                continue;
            }

            if (value.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in value.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.String)
                    {
                        AddIfMissing(identity, targetClaimType, item.GetString());
                    }
                }
            }
            else if (value.ValueKind == JsonValueKind.String)
            {
                AddIfMissing(identity, targetClaimType, value.GetString());
            }
        }
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
            using var doc = JsonDocument.Parse(bytes);
            payload = doc.RootElement.Clone();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static byte[] DecodeBase64Url(string input)
    {
        var s = input.Replace('-', '+').Replace('_', '/');
        var pad = s.Length % 4;
        if (pad is > 0)
        {
            s = s + new string('=', 4 - pad);
        }

        return Convert.FromBase64String(s);
    }

    private static void AddIfMissing(ClaimsIdentity identity, string claimType, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        if (!identity.HasClaim(claimType, value))
        {
            identity.AddClaim(new Claim(claimType, value));
        }
    }
}
