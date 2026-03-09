namespace Incursa.Integrations.WorkOS.AppAuth.Abstractions;

using System.Security.Claims;

public interface IWorkOsClaimsAccessor
{
    WorkOsClaimSet Read(ClaimsPrincipal principal);
}
