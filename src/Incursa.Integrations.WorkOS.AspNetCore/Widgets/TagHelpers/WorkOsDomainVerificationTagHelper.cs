namespace Incursa.Integrations.WorkOS.AspNetCore.Widgets.TagHelpers;

using Incursa.Integrations.WorkOS.Abstractions.Widgets;
using Incursa.Integrations.WorkOS.AspNetCore.Widgets.Services;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Logging;

[HtmlTargetElement("workos-admin-portal-domain-verification")]
public sealed class WorkOsDomainVerificationTagHelper : WorkOsWidgetTagHelperBase
{
    public WorkOsDomainVerificationTagHelper(
        IWorkOsWidgetTokenService tokenService,
        IWorkOsWidgetIdentityResolver identityResolver,
        ILogger<WorkOsDomainVerificationTagHelper> logger)
        : base(tokenService, identityResolver, logger)
    {
    }

    protected override WorkOsWidgetType WidgetType => WorkOsWidgetType.DomainVerification;
}
