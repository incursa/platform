namespace Incursa.Integrations.WorkOS.AspNetCore.Widgets.TagHelpers;

using Incursa.Integrations.WorkOS.Abstractions.Widgets;
using Incursa.Integrations.WorkOS.AspNetCore.Widgets.Services;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Logging;

[HtmlTargetElement("workos-user-security")]
public sealed class WorkOsUserSecurityTagHelper : WorkOsWidgetTagHelperBase
{
    public WorkOsUserSecurityTagHelper(
        IWorkOsWidgetTokenService tokenService,
        IWorkOsWidgetIdentityResolver identityResolver,
        ILogger<WorkOsUserSecurityTagHelper> logger)
        : base(tokenService, identityResolver, logger)
    {
    }

    protected override WorkOsWidgetType WidgetType => WorkOsWidgetType.UserSecurity;
}
