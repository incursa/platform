namespace Incursa.Platform.Access.AspNetCore;

using Microsoft.AspNetCore.Authorization;

public static class AccessAuthorizationPolicyBuilderExtensions
{
    public static AuthorizationPolicyBuilder RequireAccessRole(
        this AuthorizationPolicyBuilder builder,
        params string[] roles)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(roles);

        var normalizedRoles = NormalizeValues(roles);
        return builder.RequireAssertion(context => normalizedRoles.All(role => context.User.HasAccessRole(role)));
    }

    public static AuthorizationPolicyBuilder RequireAccessPermission(
        this AuthorizationPolicyBuilder builder,
        params string[] permissions)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(permissions);

        var normalizedPermissions = NormalizeValues(permissions);
        return builder.RequireAssertion(context => normalizedPermissions.All(permission => context.User.HasAccessPermission(permission)));
    }

    private static IReadOnlyCollection<string> NormalizeValues(IEnumerable<string> values) =>
        values.Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
}
