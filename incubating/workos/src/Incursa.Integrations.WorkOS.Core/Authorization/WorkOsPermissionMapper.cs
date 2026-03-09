namespace Incursa.Integrations.WorkOS.Core.Authorization;

using Incursa.Integrations.WorkOS.Abstractions.Authorization;
using Incursa.Integrations.WorkOS.Abstractions.Configuration;

public sealed class WorkOsPermissionMapper : IWorkOsPermissionMapper
{
    private readonly WorkOsPermissionMappingOptions _options;

    public WorkOsPermissionMapper(WorkOsPermissionMappingOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options;
    }

    public IReadOnlyCollection<string> MapToScopes(IEnumerable<string> permissions, bool strictMode, out IReadOnlyCollection<string> unknownPermissions)
    {
        ArgumentNullException.ThrowIfNull(permissions);

        HashSet<string> scopes = new(StringComparer.Ordinal);
        List<string> unknown = [];

        foreach (var permission in permissions)
        {
            if (string.IsNullOrWhiteSpace(permission))
            {
                continue;
            }

            if (_options.PermissionToScope.TryGetValue(permission.Trim(), out var mappedScopes) && mappedScopes is not null)
            {
                foreach (var mapped in mappedScopes)
                {
                    if (!string.IsNullOrWhiteSpace(mapped))
                    {
                        scopes.Add(mapped.Trim().ToLowerInvariant());
                    }
                }

                continue;
            }

            unknown.Add(permission.Trim());
        }

        unknownPermissions = unknown;
        if (strictMode && unknown.Count > 0)
        {
            return Array.Empty<string>();
        }

        return scopes.OrderBy(static x => x, StringComparer.Ordinal).ToArray();
    }
}

