namespace Incursa.Integrations.WorkOS.Abstractions.Configuration;

using Incursa.Integrations.WorkOS.Abstractions.Widgets;

public sealed class WorkOsWidgetsOptions
{
    public string ApiKey { get; set; } = string.Empty;

    public string ApiBaseUrl { get; set; } = "https://api.workos.com";

    public bool AllowAnonymousUsers { get; set; }

    public string? ThemeJson { get; set; }

    public string? ElementsJson { get; set; }

    public string? Locale { get; set; }

    public string? TextDirection { get; set; }

    public int? DialogZIndex { get; set; }

    public IDictionary<WorkOsWidgetType, string[]> WidgetScopes { get; set; } = new Dictionary<WorkOsWidgetType, string[]>();

    public OrganizationSwitcherDefaultsOptions OrganizationSwitcherDefaults { get; set; } = new();
}
