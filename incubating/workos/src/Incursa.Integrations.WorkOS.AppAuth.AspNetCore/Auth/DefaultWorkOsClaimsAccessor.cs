namespace Incursa.Integrations.WorkOS.AppAuth.AspNetCore.Auth;

using System.Security.Claims;
using Incursa.Integrations.WorkOS.AppAuth.Abstractions;
using Microsoft.Extensions.Options;

internal sealed class DefaultWorkOsClaimsAccessor : IWorkOsClaimsAccessor
{
    private readonly WorkOsAppAuthOptions options;

    public DefaultWorkOsClaimsAccessor(IOptions<WorkOsAppAuthOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        this.options = options.Value;
    }

    public WorkOsClaimSet Read(ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        var subject = GetFirst(principal, options.SubjectClaimTypes);
        var organizations = ReadSet(principal, options.OrganizationClaimTypes);
        var roles = ReadSet(principal, options.RoleClaimTypes);
        var permissions = ReadSet(principal, options.PermissionClaimTypes);

        return new WorkOsClaimSet(subject, organizations, roles, permissions);
    }

    private static string? GetFirst(ClaimsPrincipal principal, IReadOnlyList<string> claimTypes)
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

    private static IReadOnlyList<string> ReadSet(ClaimsPrincipal principal, IReadOnlyList<string> claimTypes)
    {
        HashSet<string> values = new(StringComparer.OrdinalIgnoreCase);

        foreach (var claimType in claimTypes)
        {
            foreach (var claim in principal.FindAll(claimType))
            {
                AddClaimValue(values, claim.Value);
            }
        }

        return values.OrderBy(static x => x, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static void AddClaimValue(HashSet<string> values, string? raw)
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
                using var doc = JsonDocument.Parse(trimmed);
                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in doc.RootElement.EnumerateArray())
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
                // fall back to tokenized parsing
            }
        }

        foreach (var piece in trimmed.Split(new[] { ',', ';', ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries))
        {
            values.Add(piece.Trim());
        }
    }
}
