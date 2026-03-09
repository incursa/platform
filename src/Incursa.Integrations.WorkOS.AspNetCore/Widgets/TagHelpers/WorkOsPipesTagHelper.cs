namespace Incursa.Integrations.WorkOS.AspNetCore.Widgets.TagHelpers;

using Incursa.Integrations.WorkOS.Abstractions.Widgets;
using Incursa.Integrations.WorkOS.AspNetCore.Widgets.Services;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Logging;

[HtmlTargetElement("workos-pipes")]
public sealed class WorkOsPipesTagHelper : WorkOsWidgetTagHelperBase
{
    public WorkOsPipesTagHelper(
        IWorkOsWidgetTokenService tokenService,
        IWorkOsWidgetIdentityResolver identityResolver,
        ILogger<WorkOsPipesTagHelper> logger)
        : base(tokenService, identityResolver, logger)
    {
    }

    protected override WorkOsWidgetType WidgetType => WorkOsWidgetType.Pipes;
}
