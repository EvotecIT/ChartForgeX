using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static void AppendLinearGradient(StringBuilder sb, string id, string x1, string x2, string y1, string y2, string startColor, double startOpacity, string endColor, double endOpacity) =>
        AppendLinearGradient(sb, id, x1, x2, y1, y2, SvgPaint.Plain(startColor), startOpacity, SvgPaint.Plain(endColor), endOpacity);

    private static void AppendBarSurfaceGradient(StringBuilder sb, string id, ChartColor color) =>
        AppendLinearGradient(sb, id, "0", "0", "0", "1", ChartMarkSurface.BarGradientTopBlend(color).Paint, 1, ChartMarkSurface.BarGradientBottomBlend(color).Paint, ChartVisualPrimitives.BarGradientBottomOpacity);

    private static void AppendLinearGradient(StringBuilder sb, string id, string x1, string x2, string y1, string y2, SvgPaint startColor, double startOpacity, SvgPaint endColor, double endOpacity) {
        AppendSvg(sb, writer => writer
            .StartElement("linearGradient")
            .Attribute("id", id)
            .Attribute("x1", x1)
            .Attribute("x2", x2)
            .Attribute("y1", y1)
            .Attribute("y2", y2)
            .EndStartElement()
            .StartElement("stop")
            .Attribute("offset", "0%")
            .Paint("stop-color", startColor)
            .Attribute("stop-opacity", startOpacity)
            .EndEmptyElement()
            .StartElement("stop")
            .Attribute("offset", "100%")
            .Paint("stop-color", endColor)
            .Attribute("stop-opacity", endOpacity)
            .EndEmptyElement()
            .EndElement()
            .Line());
    }

    /// <summary>
    /// Returns the <c>data-cfx-color</c> of a bar: its colour as opaque hex, or with colour variables the same CSS paint
    /// as its fill.
    /// </summary>
    private static SvgPaint DataColor(ChartColor color) => SvgPaint.Of(ChartColor.FromRgb(color.R, color.G, color.B), SvgColorRole.Series);

    /// <summary>
    /// Returns the rule that keeps state marks in their colours in forced-colours mode when the chart opts in
    /// (<see cref="ChartOptions.PinStateColorsInForcedColors"/>): marks with a status, and the hatch and outline drawn
    /// over them, get <c>forced-color-adjust:none</c>.
    /// </summary>
    private static string ForcedColorsRule(Chart chart, string id) => chart.Options.PinStateColorsInForcedColors
        ? $" #{id} [data-cfx-status],#{id} [data-cfx-role$=\"-hatch\"],#{id} [data-cfx-role$=\"-outline\"]{{forced-color-adjust:none}}"
        : string.Empty;
}
