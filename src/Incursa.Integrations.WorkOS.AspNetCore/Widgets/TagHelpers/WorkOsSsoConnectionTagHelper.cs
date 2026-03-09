namespace Incursa.Integrations.WorkOS.AspNetCore.Widgets.TagHelpers;

using Incursa.Integrations.WorkOS.Abstractions.Widgets;
using Incursa.Integrations.WorkOS.AspNetCore.Widgets.Services;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Logging;

[HtmlTargetElement("workos-admin-portal-sso-connection")]
public sealed class WorkOsSsoConnectionTagHelper : WorkOsWidgetTagHelperBase
{
    public WorkOsSsoConnectionTagHelper(
        IWorkOsWidgetTokenService tokenService,
        IWorkOsWidgetIdentityResolver identityResolver,
        ILogger<WorkOsSsoConnectionTagHelper> logger)
        : base(tokenService, identityResolver, logger)
    {
    }

    protected override WorkOsWidgetType WidgetType => WorkOsWidgetType.SsoConnection;
}
