namespace Incursa.Integrations.WorkOS.AppAuth.AspNetCore;

using Incursa.Integrations.WorkOS.AppAuth.AspNetCore.Auth;
using Microsoft.AspNetCore.Builder;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseWorkOsOrganizationContext(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.UseMiddleware<OrganizationContextMiddleware>();
    }

    public static IApplicationBuilder UseWorkOsRequireOrganization(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.UseMiddleware<RequireOrganizationSelectionMiddleware>();
    }
}
