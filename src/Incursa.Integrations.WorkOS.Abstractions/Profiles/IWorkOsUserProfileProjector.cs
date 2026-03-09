namespace Incursa.Integrations.WorkOS.Abstractions.Profiles;

using System.Security.Claims;

public interface IWorkOsUserProfileProjector
{
    void ProjectToClaims(WorkOsUserProfile profile, ClaimsIdentity identity);
}
