namespace Incursa.Integrations.WorkOS.AspNetCore.Security;

using System.Text.RegularExpressions;

public static partial class WorkOsLogRedaction
{
    private static readonly Regex JwtLikeTokenRegex = BuildJwtLikeTokenRegex();

    public static string? RedactJwtLikeTokens(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        return JwtLikeTokenRegex.Replace(value, "[REDACTED_TOKEN]");
    }

    [GeneratedRegex(@"\b[A-Za-z0-9-_]+\.[A-Za-z0-9-_]+\.[A-Za-z0-9-_]+\b", RegexOptions.Compiled | RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    private static partial Regex BuildJwtLikeTokenRegex();
}
