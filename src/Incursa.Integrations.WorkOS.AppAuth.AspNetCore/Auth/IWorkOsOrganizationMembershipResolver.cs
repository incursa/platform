namespace Incursa.Integrations.WorkOS.AppAuth.Abstractions;

using System.Security.Claims;
using Microsoft.AspNetCore.Http;

public interface IWorkOsOrganizationMembershipResolver
{
    ValueTask<bool> IsMemberAsync(ClaimsPrincipal principal, string organizationId, HttpContext httpContext, CancellationToken ct = default);
}

public sealed class PassThroughOrganizationMembershipResolver : IWorkOsOrganizationMembershipResolver
{
    public ValueTask<bool> IsMemberAsync(ClaimsPrincipal principal, string organizationId, HttpContext httpContext, CancellationToken ct = default)
    {
        _ = ct;
        ArgumentNullException.ThrowIfNull(principal);
        ArgumentNullException.ThrowIfNull(httpContext);

        var allowed = principal.FindAll("org_id")
            .Concat(principal.FindAll("organization_id"))
            .Concat(principal.FindAll("workos:org_id"))
            .Select(static c => c.Value)
            .Where(static x => !string.IsNullOrWhiteSpace(x))
            .Any(v => string.Equals(v, organizationId, StringComparison.OrdinalIgnoreCase));

        return ValueTask.FromResult(allowed);
    }
}
