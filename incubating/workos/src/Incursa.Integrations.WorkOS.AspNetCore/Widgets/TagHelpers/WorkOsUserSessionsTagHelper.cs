namespace Incursa.Integrations.WorkOS.AspNetCore.Widgets.TagHelpers;

using Incursa.Integrations.WorkOS.Abstractions.Widgets;
using Incursa.Integrations.WorkOS.AspNetCore.Widgets.Services;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Logging;

[HtmlTargetElement("workos-user-sessions")]
public sealed class WorkOsUserSessionsTagHelper : WorkOsWidgetTagHelperBase
{
    private readonly IWorkOsCurrentSessionIdResolver currentSessionIdResolver;

    public WorkOsUserSessionsTagHelper(
        IWorkOsWidgetTokenService tokenService,
        IWorkOsWidgetIdentityResolver identityResolver,
        IWorkOsCurrentSessionIdResolver currentSessionIdResolver,
        ILogger<WorkOsUserSessionsTagHelper> logger)
        : base(tokenService, identityResolver, logger)
    {
        ArgumentNullException.ThrowIfNull(currentSessionIdResolver);
        this.currentSessionIdResolver = currentSessionIdResolver;
    }

    [HtmlAttributeName("current-session-id")]
    public string? CurrentSessionId { get; set; }

    protected override WorkOsWidgetType WidgetType => WorkOsWidgetType.UserSessions;

    protected override async Task<WidgetClientConfig> BuildClientConfigAsync(
        string token,
        WorkOsWidgetIdentity? identity,
        CancellationToken cancellationToken)
    {
        var sessionId = CurrentSessionId;
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            sessionId = await currentSessionIdResolver.ResolveAsync(cancellationToken).ConfigureAwait(false);
        }

        if (string.IsNullOrWhiteSpace(sessionId))
        {
            throw new InvalidOperationException(
                "workos-user-sessions requires current-session-id or an IWorkOsCurrentSessionIdResolver that can resolve one.");
        }

        return new WidgetClientConfig
        {
            Widget = WidgetType.ToWidgetName(),
            AuthToken = token,
            ThemeJson = ResolveThemeJson(),
            ElementsJson = ResolveElementsJson(),
            Locale = ResolveLocale(),
            TextDirection = ResolveTextDirection(),
            DialogZIndex = ResolveDialogZIndex(),
            CurrentSessionId = sessionId,
        };
    }
}
