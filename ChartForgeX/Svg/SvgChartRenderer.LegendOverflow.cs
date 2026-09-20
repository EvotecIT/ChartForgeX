using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static void DrawLegendOverflow(SvgMarkupWriter writer, Chart chart, ChartRect area, double y, int omitted) {
        var style = chart.Options.LegendStyle;
        var label = LegendRowBudget.Summary(omitted);
        var fontSize = TextFontSizeForSvgWidth(label, Math.Max(8, area.Width), StyleFontSize(style, chart.Options.Theme.LegendFontSize));
        var rowWidth = Math.Min(area.Width, EstimateSvgStyledTextWidth(chart, label, fontSize, style, emphasized: true));
        var x = area.X + LegendRowX(chart.Options.LegendPosition, area, rowWidth);
        writer.StartElement("g").Attribute("data-cfx-role", "legend-row").EndStartElement();
        writer.StartElement("text").Attribute("data-cfx-role", "legend-overflow").Attribute("data-cfx-omitted", omitted)
            .Attribute("x", x).Attribute("y", y).Attribute("font-size", fontSize)
            .Attribute("font-family", SvgFontFamilyAttributeValue(StyleFontFamily(chart, style)))
            .Attribute("fill", StyleColor(style, chart.Options.Theme.MutedText).ToCss())
            .Attribute("font-weight", StyleWeight(style, "600"));
        WriteSvgTextStyleAttributes(writer, style);
        WriteSvgStyledTextContent(writer, style, label).EndElement().EndElement().Line();
    }
}
