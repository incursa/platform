namespace Incursa.Integrations.WorkOS.AppAuth.AspNetCore.Auth;

using Incursa.Integrations.WorkOS.AppAuth.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class RequirePermissionAttribute : AuthorizeAttribute
{
    public RequirePermissionAttribute(string permission)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(permission);
        Policy = $"perm:{permission}";
    }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class RequireAnyPermissionAttribute : AuthorizeAttribute
{
    public RequireAnyPermissionAttribute(params string[] permissions)
    {
        ArgumentNullException.ThrowIfNull(permissions);
        var filtered = permissions
            .Where(static x => !string.IsNullOrWhiteSpace(x))
            .Select(static x => x.Trim())
            .ToArray();

        if (filtered.Length == 0)
        {
            throw new ArgumentException("At least one permission is required.", nameof(permissions));
        }

        Policy = $"perm:any:{string.Join("|", filtered)}";
    }
}

internal sealed class PermissionPolicyPrefixPostConfigure : IPostConfigureOptions<WorkOsAppAuthOptions>
{
    public void PostConfigure(string? name, WorkOsAppAuthOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (string.IsNullOrWhiteSpace(options.PermissionPolicyPrefix))
        {
            options.PermissionPolicyPrefix = "perm:";
        }
    }
}
