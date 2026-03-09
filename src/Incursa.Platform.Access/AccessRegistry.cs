#pragma warning disable MA0048
namespace Incursa.Platform.Access;

public sealed class AccessRegistryBuilder
{
    private readonly Dictionary<AccessPermissionId, AccessPermissionDefinition> permissions = new();
    private readonly Dictionary<AccessRoleId, AccessRoleDefinition> roles = new();

    public AccessRegistryBuilder AddPermission(string permissionId, string displayName, string? description = null) =>
        AddPermission(new AccessPermissionDefinition(new AccessPermissionId(permissionId), displayName, description));

    public AccessRegistryBuilder AddPermission(AccessPermissionDefinition permission)
    {
        ArgumentNullException.ThrowIfNull(permission);

        if (!permissions.TryAdd(permission.Id, permission))
        {
            throw new InvalidOperationException($"Permission '{permission.Id}' is already registered.");
        }

        return this;
    }

    public AccessRegistryBuilder AddRole(string roleId, string displayName, params string[] permissionIds)
    {
        ArgumentNullException.ThrowIfNull(permissionIds);

        return AddRole(
            new AccessRoleDefinition(
                new AccessRoleId(roleId),
                displayName,
                permissionIds.Select(static item => new AccessPermissionId(item)).ToArray()));
    }

    public AccessRegistryBuilder AddRole(AccessRoleDefinition role)
    {
        ArgumentNullException.ThrowIfNull(role);

        foreach (var permissionId in role.Permissions)
        {
            if (!permissions.ContainsKey(permissionId))
            {
                throw new InvalidOperationException(
                    $"Role '{role.Id}' references unknown permission '{permissionId}'.");
            }
        }

        if (!roles.TryAdd(role.Id, NormalizeRole(role)))
        {
            throw new InvalidOperationException($"Role '{role.Id}' is already registered.");
        }

        return this;
    }

    public IAccessRegistry Build(string version)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(version);

        Dictionary<string, AccessPermissionDefinition> permissionAliases = new(StringComparer.Ordinal);
        Dictionary<string, AccessRoleDefinition> roleAliases = new(StringComparer.Ordinal);

        foreach (var permission in permissions.Values)
        {
            RegisterAliases(permission.ProviderAliases, permission, permissionAliases);
        }

        foreach (var role in roles.Values)
        {
            RegisterAliases(role.ProviderAliases, role, roleAliases);
        }

        return new AccessRegistry(
            version.Trim(),
            permissions.Values.OrderBy(static item => item.Id.Value, StringComparer.Ordinal).ToArray(),
            roles.Values.OrderBy(static item => item.Id.Value, StringComparer.Ordinal).ToArray(),
            permissionAliases,
            roleAliases);
    }

    private static AccessRoleDefinition NormalizeRole(AccessRoleDefinition role)
    {
        var sortedPermissions = role.Permissions
            .Distinct()
            .OrderBy(static item => item.Value, StringComparer.Ordinal)
            .ToArray();

        return new AccessRoleDefinition(role.Id, role.DisplayName, sortedPermissions, role.Description, role.ProviderAliases);
    }

    private static void RegisterAliases<TDefinition>(
        IReadOnlyDictionary<string, string> aliases,
        TDefinition definition,
        IDictionary<string, TDefinition> target)
    {
        foreach ((string providerAliasKey, string aliasValue) in aliases)
        {
            var key = MakeAliasKey(providerAliasKey, aliasValue);
            if (!target.TryAdd(key, definition))
            {
                throw new InvalidOperationException($"Provider alias '{providerAliasKey}:{aliasValue}' is already registered.");
            }
        }
    }

    private static string MakeAliasKey(string providerAliasKey, string aliasValue)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerAliasKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(aliasValue);
        return providerAliasKey.Trim() + "::" + aliasValue.Trim();
    }
}

public sealed class AccessRegistry : IAccessRegistry
{
    private readonly IReadOnlyDictionary<AccessPermissionId, AccessPermissionDefinition> permissions;
    private readonly IReadOnlyDictionary<AccessRoleId, AccessRoleDefinition> roles;
    private readonly IReadOnlyDictionary<string, AccessPermissionDefinition> permissionAliases;
    private readonly IReadOnlyDictionary<string, AccessRoleDefinition> roleAliases;

    internal AccessRegistry(
        string version,
        IReadOnlyCollection<AccessPermissionDefinition> permissions,
        IReadOnlyCollection<AccessRoleDefinition> roles,
        IReadOnlyDictionary<string, AccessPermissionDefinition> permissionAliases,
        IReadOnlyDictionary<string, AccessRoleDefinition> roleAliases)
    {
        this.permissions = permissions.ToDictionary(static item => item.Id);
        this.roles = roles.ToDictionary(static item => item.Id);
        this.permissionAliases = permissionAliases;
        this.roleAliases = roleAliases;
        Snapshot = new AccessRegistrySnapshot(version, permissions.ToArray(), roles.ToArray());
    }

    public AccessRegistrySnapshot Snapshot { get; }

    public bool TryGetPermission(AccessPermissionId permissionId, [NotNullWhen(true)] out AccessPermissionDefinition? permission) =>
        permissions.TryGetValue(permissionId, out permission);

    public bool TryGetRole(AccessRoleId roleId, [NotNullWhen(true)] out AccessRoleDefinition? role) =>
        roles.TryGetValue(roleId, out role);

    public bool TryGetPermissionByProviderAlias(
        string providerAliasKey,
        string aliasValue,
        [NotNullWhen(true)] out AccessPermissionDefinition? permission) =>
        permissionAliases.TryGetValue(providerAliasKey.Trim() + "::" + aliasValue.Trim(), out permission);

    public bool TryGetRoleByProviderAlias(
        string providerAliasKey,
        string aliasValue,
        [NotNullWhen(true)] out AccessRoleDefinition? role) =>
        roleAliases.TryGetValue(providerAliasKey.Trim() + "::" + aliasValue.Trim(), out role);
}
#pragma warning restore MA0048
