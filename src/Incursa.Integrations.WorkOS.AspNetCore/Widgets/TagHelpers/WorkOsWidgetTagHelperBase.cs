namespace Incursa.Integrations.WorkOS.AspNetCore.Widgets.TagHelpers;

using Incursa.Integrations.WorkOS.Abstractions.Configuration;
using Incursa.Integrations.WorkOS.Abstractions.Widgets;
using Incursa.Integrations.WorkOS.AspNetCore.Widgets.Services;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public abstract class WorkOsWidgetTagHelperBase : TagHelper
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly IWorkOsWidgetTokenService tokenService;
    private readonly IWorkOsWidgetIdentityResolver identityResolver;
    private readonly ILogger logger;

    protected WorkOsWidgetTagHelperBase(
        IWorkOsWidgetTokenService tokenService,
        IWorkOsWidgetIdentityResolver identityResolver,
        ILogger logger)
    {
        this.tokenService = tokenService;
        this.identityResolver = identityResolver;
        this.logger = logger;
    }

    [HtmlAttributeName("id")]
    public string? Id { get; set; }

    [HtmlAttributeName("class")]
    public string? Class { get; set; }

    [HtmlAttributeName("auth-token")]
    public string? AuthToken { get; set; }

    [HtmlAttributeName("theme-json")]
    public string? ThemeJson { get; set; }

    [HtmlAttributeName("elements-json")]
    public string? ElementsJson { get; set; }

    [HtmlAttributeName("locale")]
    public string? Locale { get; set; }

    [HtmlAttributeName("text-direction")]
    public string? TextDirection { get; set; }

    [HtmlAttributeName("dialog-z-index")]
    public int? DialogZIndex { get; set; }

    [ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; } = default!;

    protected abstract WorkOsWidgetType WidgetType { get; }

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "div";
        output.TagMode = TagMode.StartTagAndEndTag;

        if (!string.IsNullOrWhiteSpace(Id))
        {
            output.Attributes.SetAttribute("id", Id);
        }

        if (!string.IsNullOrWhiteSpace(Class))
        {
            output.Attributes.SetAttribute("class", Class);
        }

        try
        {
            var cancellationToken = ViewContext.HttpContext.RequestAborted;
            var identity = await ResolveIdentityAsync(cancellationToken).ConfigureAwait(false);
            var token = await ResolveTokenAsync(identity, cancellationToken).ConfigureAwait(false);
            var clientConfig = await BuildClientConfigAsync(token, identity, cancellationToken).ConfigureAwait(false);
            var serializedConfig = JsonSerializer.Serialize(clientConfig, SerializerOptions);

            output.Attributes.SetAttribute("data-workos-widget-host", "true");
            output.Attributes.SetAttribute("data-workos-widget", WidgetType.ToWidgetName());
            output.Attributes.SetAttribute("data-workos-widget-config", serializedConfig);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to render WorkOS widget {Widget}.", WidgetType);
            output.Attributes.SetAttribute("data-workos-widget-error", "true");
            output.Content.SetContent($"WorkOS widget failed to initialize ({WidgetType.ToWidgetName()}).");
        }
    }

    protected virtual Task<WidgetClientConfig> BuildClientConfigAsync(
        string token,
        WorkOsWidgetIdentity? identity,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(new WidgetClientConfig
        {
            Widget = WidgetType.ToWidgetName(),
            AuthToken = token,
            ThemeJson = ResolveThemeJson(),
            ElementsJson = ResolveElementsJson(),
            Locale = ResolveLocale(),
            TextDirection = ResolveTextDirection(),
            DialogZIndex = ResolveDialogZIndex(),
        });
    }

    protected string? ResolveThemeJson()
    {
        if (!string.IsNullOrWhiteSpace(ThemeJson))
        {
            return ThemeJson;
        }

        var options = ViewContext.HttpContext.RequestServices.GetRequiredService<IOptions<WorkOsWidgetsOptions>>().Value;
        return options.ThemeJson;
    }

    protected string? ResolveElementsJson()
    {
        if (!string.IsNullOrWhiteSpace(ElementsJson))
        {
            return ElementsJson;
        }

        var options = ViewContext.HttpContext.RequestServices.GetRequiredService<IOptions<WorkOsWidgetsOptions>>().Value;
        return options.ElementsJson;
    }

    protected string? ResolveLocale()
    {
        if (!string.IsNullOrWhiteSpace(Locale))
        {
            return Locale;
        }

        var options = ViewContext.HttpContext.RequestServices.GetRequiredService<IOptions<WorkOsWidgetsOptions>>().Value;
        return options.Locale;
    }

    protected string? ResolveTextDirection()
    {
        var direction = !string.IsNullOrWhiteSpace(TextDirection)
            ? TextDirection
            : ViewContext.HttpContext.RequestServices.GetRequiredService<IOptions<WorkOsWidgetsOptions>>().Value.TextDirection;

        if (string.IsNullOrWhiteSpace(direction))
        {
            return null;
        }

        if (string.Equals(direction, "ltr", StringComparison.OrdinalIgnoreCase))
        {
            return "ltr";
        }

        if (string.Equals(direction, "rtl", StringComparison.OrdinalIgnoreCase))
        {
            return "rtl";
        }

        throw new InvalidOperationException("text-direction must be 'ltr' or 'rtl'.");
    }

    protected int? ResolveDialogZIndex()
    {
        var zIndex = DialogZIndex
            ?? ViewContext.HttpContext.RequestServices.GetRequiredService<IOptions<WorkOsWidgetsOptions>>().Value.DialogZIndex;

        if (zIndex is <= 0)
        {
            throw new InvalidOperationException("dialog-z-index must be greater than 0.");
        }

        return zIndex;
    }

    private async Task<WorkOsWidgetIdentity?> ResolveIdentityAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(AuthToken))
        {
            return null;
        }

        var options = ViewContext.HttpContext.RequestServices.GetRequiredService<IOptions<WorkOsWidgetsOptions>>().Value;
        if (!options.AllowAnonymousUsers && ViewContext.HttpContext.User.Identity?.IsAuthenticated != true)
        {
            throw new InvalidOperationException("WorkOS widget rendering requires an authenticated user unless auth-token is provided.");
        }

        return await identityResolver.ResolveAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<string> ResolveTokenAsync(WorkOsWidgetIdentity? identity, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(AuthToken))
        {
            return AuthToken;
        }

        if (identity is null)
        {
            throw new InvalidOperationException("A WorkOS identity could not be resolved.");
        }

        return await tokenService.CreateTokenAsync(WidgetType, identity, cancellationToken).ConfigureAwait(false);
    }
}
