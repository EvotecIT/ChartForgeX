using System;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static TextStyleOverride GraphiteTextStyle(Chart chart, string role) =>
        role.StartsWith("slice-legend", StringComparison.Ordinal) ? chart.Options.LegendStyle
        : role.Contains("axis") || role.StartsWith("heatmap-scale", StringComparison.Ordinal) || role == "gauge-min-label" || role == "gauge-max-label" ? chart.Options.TickLabelStyle
        : chart.Options.DataLabelStyle;

    private static void GraphiteEndText(SvgMarkupWriter writer, Chart chart, string role, string text, double x, double y, ChartColor color, double size, double width, string weight, TextStyleOverride? style = null) {
        style ??= GraphiteTextStyle(chart, role);
        size = StyleFontSize(style, size);
        var fittedSize = TextFontSizeForSvgWidth(chart, text, Math.Max(8, width), size, style, minFontSize: Math.Min(8, size));
        var fittedText = TrimSvgLabelToWidth(chart, text, fittedSize, Math.Max(8, width), style);
        writer.StartElement("text").Attribute("data-cfx-role", role).Attribute("x", x).Attribute("y", y).Attribute("text-anchor", "end").Attribute("fill", StyleColor(style, color).ToCss()).Attribute("font-family", SvgFontFamilyAttributeValue(StyleFontFamily(chart, style))).Attribute("font-size", fittedSize).Attribute("font-weight", StyleWeight(style, weight));
        WriteSvgTextStyleAttributes(writer, style);
        WriteSvgStyledTextContent(writer, style, fittedText).EndElement();
    }

    private static void DrawSvgTextLeft(SvgMarkupWriter writer, Chart chart, string role, string text, double x, double y, ChartColor fill, double size, double width, string weight, TextStyleOverride? style = null) {
        var builder = new StringBuilder();
        style ??= GraphiteTextStyle(chart, role);
        DrawSvgTextLeft(builder, chart, role, text, x, y, fill, StyleFontSize(style,size), width, weight, style);
        writer.Raw(builder.ToString());
    }
}
