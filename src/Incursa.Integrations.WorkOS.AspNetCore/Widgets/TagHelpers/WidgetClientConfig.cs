namespace Incursa.Integrations.WorkOS.AspNetCore.Widgets.TagHelpers;

public sealed class WidgetClientConfig
{
    required public string Widget { get; init; }

    // Backward-compatibility alias for older runtimes that read `widgetType`.
    public string WidgetType => Widget;

    required public string AuthToken { get; init; }

    public string? ThemeJson { get; init; }

    public string? ElementsJson { get; init; }

    public string? Locale { get; init; }

    public string? TextDirection { get; init; }

    public int? DialogZIndex { get; init; }

    public string? CurrentSessionId { get; init; }

    public OrganizationSwitcherClientConfig? OrganizationSwitcher { get; init; }
}

public sealed class OrganizationSwitcherClientConfig
{
    public string? RedirectTemplate { get; init; }

    public string? RedirectFixedRoute { get; init; }

    public string? SwitchEndpoint { get; init; }

    public string? CreateOrganizationUrl { get; init; }

    public string? CreateOrganizationLabel { get; init; }

    public string? CreateOrganizationTarget { get; init; }

    public string? CurrentOrganizationId { get; init; }

    public string? CurrentOrganizationExternalId { get; init; }

    public bool PreferExternalId { get; init; }

    public IReadOnlyDictionary<string, string>? ExternalIdMap { get; init; }
}
