namespace Incursa.Integrations.WorkOS.AspNetCore.Widgets.TagHelpers;

using Incursa.Integrations.WorkOS.Abstractions.Widgets;
using Incursa.Integrations.WorkOS.AspNetCore.Widgets.Services;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Logging;

[HtmlTargetElement("workos-api-keys")]
public sealed class WorkOsApiKeysTagHelper : WorkOsWidgetTagHelperBase
{
    public WorkOsApiKeysTagHelper(
        IWorkOsWidgetTokenService tokenService,
        IWorkOsWidgetIdentityResolver identityResolver,
        ILogger<WorkOsApiKeysTagHelper> logger)
        : base(tokenService, identityResolver, logger)
    {
    }

    protected override WorkOsWidgetType WidgetType => WorkOsWidgetType.ApiKeys;
}
