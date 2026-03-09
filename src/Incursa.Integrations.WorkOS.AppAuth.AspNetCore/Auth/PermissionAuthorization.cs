namespace Incursa.Integrations.WorkOS.AppAuth.AspNetCore.Auth;

using Incursa.Integrations.WorkOS.AppAuth.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

public sealed class PermissionRequirement : IAuthorizationRequirement
{
    public PermissionRequirement(string permission)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(permission);
        Permission = permission;
    }

    public string Permission { get; }
}

public sealed class AnyPermissionRequirement : IAuthorizationRequirement
{
    public AnyPermissionRequirement(IReadOnlyCollection<string> permissions)
    {
        ArgumentNullException.ThrowIfNull(permissions);
        Permissions = permissions;
    }

    public IReadOnlyCollection<string> Permissions { get; }
}

internal sealed class PermissionHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IWorkOsClaimsAccessor claimsAccessor;

    public PermissionHandler(IWorkOsClaimsAccessor claimsAccessor)
    {
        ArgumentNullException.ThrowIfNull(claimsAccessor);
        this.claimsAccessor = claimsAccessor;
    }

    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        var claimSet = this.claimsAccessor.Read(context.User);
        if (claimSet.Permissions.Contains(requirement.Permission, StringComparer.OrdinalIgnoreCase))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

internal sealed class AnyPermissionHandler : AuthorizationHandler<AnyPermissionRequirement>
{
    private readonly IWorkOsClaimsAccessor claimsAccessor;

    public AnyPermissionHandler(IWorkOsClaimsAccessor claimsAccessor)
    {
        ArgumentNullException.ThrowIfNull(claimsAccessor);
        this.claimsAccessor = claimsAccessor;
    }

    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, AnyPermissionRequirement requirement)
    {
        var claimSet = this.claimsAccessor.Read(context.User);
        if (requirement.Permissions.Any(permission => claimSet.Permissions.Contains(permission, StringComparer.OrdinalIgnoreCase)))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

internal sealed class PermissionPolicyProvider : DefaultAuthorizationPolicyProvider
{
    private readonly WorkOsAppAuthOptions options;

    public PermissionPolicyProvider(IOptions<AuthorizationOptions> authorizationOptions, IOptions<WorkOsAppAuthOptions> options)
        : base(authorizationOptions)
    {
        ArgumentNullException.ThrowIfNull(options);
        this.options = options.Value;
    }

    public override Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (!policyName.StartsWith(this.options.PermissionPolicyPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return base.GetPolicyAsync(policyName);
        }

        var body = policyName[this.options.PermissionPolicyPrefix.Length..];
        if (body.StartsWith("any:", StringComparison.OrdinalIgnoreCase))
        {
            var permissions = body["any:".Length..]
                .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(static x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            var anyPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new AnyPermissionRequirement(permissions))
                .Build();

            return Task.FromResult<AuthorizationPolicy?>(anyPolicy);
        }

        var policy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(new PermissionRequirement(body))
            .Build();

        return Task.FromResult<AuthorizationPolicy?>(policy);
    }
}
