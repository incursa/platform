namespace Incursa.Integrations.WorkOS.Abstractions.Authorization;

public interface IWorkOsPermissionMapper
{
    IReadOnlyCollection<string> MapToScopes(IEnumerable<string> permissions, bool strictMode, out IReadOnlyCollection<string> unknownPermissions);
}

