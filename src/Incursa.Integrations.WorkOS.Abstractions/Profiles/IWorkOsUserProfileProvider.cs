namespace Incursa.Integrations.WorkOS.Abstractions.Profiles;

using System.Security.Claims;

public interface IWorkOsUserProfileProvider
{
    ValueTask<WorkOsUserProfile?> GetProfileAsync(ClaimsPrincipal principal, CancellationToken ct = default);
}
