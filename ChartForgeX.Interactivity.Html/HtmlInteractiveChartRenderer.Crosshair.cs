using System.Globalization;
using System.Text;

namespace ChartForgeX.Interactivity.Html;

public sealed partial class HtmlInteractiveChartRenderer {
    private static string CrosshairInteractionTokens(HtmlChartCrosshairOptions options) {
        if (!options.HasLineOverrides) return string.Empty;
        var style = new StringBuilder();
        if (options.Stroke.HasValue) style.Append(";--cfx-crosshair-stroke:").Append(options.Stroke.Value.ToCss());
        if (options.StrokeWidth.HasValue) style.Append(";--cfx-crosshair-stroke-width:")
            .Append(options.StrokeWidth.Value.ToString("R", CultureInfo.InvariantCulture)).Append("px");
        if (options.LineStyle.HasValue) style.Append(";--cfx-crosshair-line-style:").Append(options.LineStyle.Value.ToString().ToLowerInvariant());
        return style.ToString();
    }
}
