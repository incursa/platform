namespace Incursa.Integrations.WorkOS.Abstractions.Configuration;

public sealed class WorkOsPermissionMappingOptions
{
    private readonly Dictionary<string, string[]> permissionToScope = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, string[]> PermissionToScope => permissionToScope;

    public static WorkOsPermissionMappingOptions CreateDefaultArtifacts()
    {
        var options = new WorkOsPermissionMappingOptions();
        options.permissionToScope["nuget.read"] = ["nuget.read"];
        options.permissionToScope["nuget.push"] = ["nuget.push"];
        options.permissionToScope["artifacts:nuget:read"] = ["nuget.read", "artifacts:nuget:read"];
        options.permissionToScope["artifacts:nuget:push"] = ["nuget.push", "artifacts:nuget:push"];
        options.permissionToScope["apt.read"] = ["apt.read"];
        options.permissionToScope["apt.push"] = ["apt.push"];
        options.permissionToScope["artifacts:apt:read"] = ["apt.read", "artifacts:apt:read"];
        options.permissionToScope["artifacts:apt:push"] = ["apt.push", "artifacts:apt:push"];
        options.permissionToScope["raw.read"] = ["raw.read"];
        options.permissionToScope["raw.push"] = ["raw.push"];
        options.permissionToScope["artifacts:raw:read"] = ["raw.read", "artifacts:raw:read"];
        options.permissionToScope["artifacts:raw:push"] = ["raw.push", "artifacts:raw:push"];
        options.permissionToScope["artifacts:admin:read"] = ["artifacts:admin:read"];
        options.permissionToScope["artifacts:admin:write"] = ["artifacts:admin:write", "admin"];
        options.permissionToScope["admin"] = ["admin"];
        return options;
    }
}

