namespace Incursa.Integrations.WorkOS.AspNetCore.Widgets.TagHelpers;

using Incursa.Integrations.WorkOS.Abstractions.Configuration;
using Incursa.Integrations.WorkOS.Abstractions.Widgets;
using Incursa.Integrations.WorkOS.AspNetCore.Widgets.Services;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

[HtmlTargetElement("workos-organization-switcher")]
public sealed class WorkOsOrganizationSwitcherTagHelper : WorkOsWidgetTagHelperBase
{
    private readonly IOptions<WorkOsWidgetsOptions> options;

    public WorkOsOrganizationSwitcherTagHelper(
        IWorkOsWidgetTokenService tokenService,
        IWorkOsWidgetIdentityResolver identityResolver,
        IOptions<WorkOsWidgetsOptions> options,
        ILogger<WorkOsOrganizationSwitcherTagHelper> logger)
        : base(tokenService, identityResolver, logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        this.options = options;
    }

    [HtmlAttributeName("redirect-template")]
    public string? RedirectTemplate { get; set; }

    [HtmlAttributeName("redirect-fixed-route")]
    public string? RedirectFixedRoute { get; set; }

    [HtmlAttributeName("switch-endpoint")]
    public string? SwitchEndpoint { get; set; }

    [HtmlAttributeName("create-organization-url")]
    public string? CreateOrganizationUrl { get; set; }

    [HtmlAttributeName("create-organization-label")]
    public string? CreateOrganizationLabel { get; set; }

    [HtmlAttributeName("create-organization-target")]
    public string? CreateOrganizationTarget { get; set; }

    [HtmlAttributeName("prefer-external-id")]
    public bool? PreferExternalId { get; set; }

    [HtmlAttributeName("external-id-map-json")]
    public string? ExternalIdMapJson { get; set; }

    protected override WorkOsWidgetType WidgetType => WorkOsWidgetType.OrganizationSwitcher;

    protected override Task<WidgetClientConfig> BuildClientConfigAsync(
        string token,
        WorkOsWidgetIdentity? identity,
        CancellationToken cancellationToken)
    {
        var defaults = options.Value.OrganizationSwitcherDefaults;
        var preferExternalId = PreferExternalId
            ?? defaults.IdentifierPreference == OrganizationIdentifierPreference.ExternalIdPreferred;
        var externalIdMap = ParseExternalIdMap();
        var effectiveTemplate = ResolveRedirectTemplate(defaults, identity);
        var effectiveFixedRoute = ResolveFixedRoute(defaults);

        return Task.FromResult(new WidgetClientConfig
        {
            Widget = WidgetType.ToWidgetName(),
            AuthToken = token,
            ThemeJson = ResolveThemeJson(),
            ElementsJson = ResolveElementsJson(),
            Locale = ResolveLocale(),
            TextDirection = ResolveTextDirection(),
            DialogZIndex = ResolveDialogZIndex(),
            OrganizationSwitcher = new OrganizationSwitcherClientConfig
            {
                RedirectTemplate = effectiveTemplate,
                RedirectFixedRoute = effectiveFixedRoute,
                SwitchEndpoint = ResolveSwitchEndpoint(),
                CreateOrganizationUrl = CreateOrganizationUrl,
                CreateOrganizationLabel = ResolveCreateOrganizationLabel(),
                CreateOrganizationTarget = ResolveCreateOrganizationTarget(),
                PreferExternalId = preferExternalId,
                CurrentOrganizationId = identity?.OrganizationId,
                CurrentOrganizationExternalId = identity?.OrganizationExternalId,
                ExternalIdMap = externalIdMap,
            },
        });
    }

    private IReadOnlyDictionary<string, string>? ParseExternalIdMap()
    {
        if (string.IsNullOrWhiteSpace(ExternalIdMapJson))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(ExternalIdMapJson);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("external-id-map-json must be valid JSON object syntax.", ex);
        }
    }

    private string? ResolveRedirectTemplate(OrganizationSwitcherDefaultsOptions defaults, WorkOsWidgetIdentity? identity)
    {
        if (!string.IsNullOrWhiteSpace(RedirectTemplate))
        {
            return RedirectTemplate;
        }

        if (defaults.RedirectMode == OrganizationSwitcherRedirectMode.Template
            && !string.IsNullOrWhiteSpace(defaults.DefaultTemplate))
        {
            return defaults.DefaultTemplate;
        }

        if (!defaults.PreserveCurrentPath)
        {
            return null;
        }

        var currentPath = ViewContext.HttpContext.Request.Path + ViewContext.HttpContext.Request.QueryString;
        if (string.IsNullOrWhiteSpace(currentPath))
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(identity?.OrganizationExternalId)
            && currentPath.Contains(identity.OrganizationExternalId, StringComparison.OrdinalIgnoreCase))
        {
            return currentPath.Replace(identity.OrganizationExternalId, "{externalId}", StringComparison.OrdinalIgnoreCase);
        }

        if (!string.IsNullOrWhiteSpace(identity?.OrganizationId)
            && currentPath.Contains(identity.OrganizationId, StringComparison.OrdinalIgnoreCase))
        {
            return currentPath.Replace(identity.OrganizationId, "{organizationId}", StringComparison.OrdinalIgnoreCase);
        }

        var separator = currentPath.Contains('?') ? '&' : '?';
        return $"{currentPath}{separator}organizationId={{organizationId}}";
    }

    private string? ResolveFixedRoute(OrganizationSwitcherDefaultsOptions defaults)
    {
        if (!string.IsNullOrWhiteSpace(RedirectFixedRoute))
        {
            return RedirectFixedRoute;
        }

        if (defaults.RedirectMode == OrganizationSwitcherRedirectMode.FixedRoute)
        {
            return defaults.FixedRoute;
        }

        return null;
    }

    private string? ResolveSwitchEndpoint()
    {
        if (string.IsNullOrWhiteSpace(SwitchEndpoint))
        {
            return null;
        }

        return SwitchEndpoint;
    }

    private string ResolveCreateOrganizationLabel()
    {
        if (!string.IsNullOrWhiteSpace(CreateOrganizationLabel))
        {
            return CreateOrganizationLabel;
        }

        return "Create organization";
    }

    private string ResolveCreateOrganizationTarget()
    {
        if (!string.IsNullOrWhiteSpace(CreateOrganizationTarget))
        {
            return CreateOrganizationTarget;
        }

        return "_self";
    }
}
