namespace Incursa.Integrations.WorkOS.AspNetCore.Widgets.TagHelpers;

using Microsoft.AspNetCore.Razor.TagHelpers;

[HtmlTargetElement("workos-widgets-assets")]
public sealed class WorkOsWidgetsAssetsTagHelper : TagHelper
{
    private const string CssRenderedKey = "__WorkOsWidgetsAssetsCssRendered";
    private const string JsRenderedKey = "__WorkOsWidgetsAssetsJsRendered";

    [HtmlAttributeName("include-css")]
    public bool IncludeCss { get; set; } = true;

    [HtmlAttributeName("include-js")]
    public bool IncludeJs { get; set; } = true;

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = null;
        var parts = new List<string>();

        if (IncludeCss && !context.Items.ContainsKey(CssRenderedKey))
        {
            context.Items[CssRenderedKey] = true;
            parts.Add("<link rel=\"stylesheet\" href=\"/_content/Incursa.Integrations.WorkOS.AspNetCore/workos-widgets/workos-widgets.css\" />");
        }

        if (IncludeJs && !context.Items.ContainsKey(JsRenderedKey))
        {
            context.Items[JsRenderedKey] = true;
            parts.Add("<script type=\"module\" src=\"/_content/Incursa.Integrations.WorkOS.AspNetCore/workos-widgets/workos-widgets.js\"></script>");
        }

        if (parts.Count == 0)
        {
            output.SuppressOutput();
            return;
        }

        output.Content.SetHtmlContent(string.Join(string.Empty, parts));
    }
}
