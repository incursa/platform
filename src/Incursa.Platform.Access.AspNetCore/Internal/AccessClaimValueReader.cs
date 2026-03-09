namespace Incursa.Platform.Access.AspNetCore;

using System.Security.Claims;

internal static class AccessClaimValueReader
{
    public static string? ReadFirst(ClaimsPrincipal principal, IReadOnlyList<string> claimTypes)
    {
        foreach (var claimType in claimTypes)
        {
            var value = principal.FindFirst(claimType)?.Value;
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }

    public static IReadOnlyList<string> ReadSet(ClaimsPrincipal principal, IReadOnlyList<string> claimTypes)
    {
        HashSet<string> values = new(StringComparer.OrdinalIgnoreCase);

        foreach (var claimType in claimTypes)
        {
            foreach (var claim in principal.FindAll(claimType))
            {
                Add(values, claim.Value);
            }
        }

        return values.OrderBy(static value => value, StringComparer.OrdinalIgnoreCase).ToArray();
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
                // Fall back to tokenized parsing for malformed arrays.
            }
        }

        foreach (var part in trimmed.Split(new[] { ',', ';', ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries))
        {
            values.Add(part.Trim());
        }
    }
}
