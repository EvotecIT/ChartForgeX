using System;
using System.Globalization;
using ChartForgeX.Html;

namespace ChartForgeX.Interactivity.Html;

public sealed partial class HtmlInteractiveChartRenderer {
    private static void WriteHighlightOptions(HtmlMarkupWriter writer, HtmlChartHighlightOptions options) {
        if (!Enum.IsDefined(typeof(HtmlChartHighlightMode), options.Mode)) throw new ArgumentOutOfRangeException(nameof(options.Mode));
        if (options.Mode == HtmlChartHighlightMode.Default) return;
        writer.Attribute("data-cfx-highlight-mode", "root-branch");
        WriteHighlightStyle(writer, "item", options.HighlightedItem);
        WriteHighlightStyle(writer, "branch", options.HighlightedBranch);
        WriteHighlightStyle(writer, "other-item", options.UnhighlightedItem);
        WriteHighlightStyle(writer, "other-branch", options.UnhighlightedBranch);
    }

    private static void WriteHighlightStyle(HtmlMarkupWriter writer, string group, HtmlChartHighlightStyle style) {
        var prefix = "data-cfx-highlight-" + group + "-";
        writer.Attribute(prefix + "stroke", style.Stroke?.ToCss())
            .Attribute(prefix + "stroke-width", style.StrokeWidth?.ToString("R", CultureInfo.InvariantCulture))
            .Attribute(prefix + "opacity", style.Opacity?.ToString("R", CultureInfo.InvariantCulture));
    }
}
