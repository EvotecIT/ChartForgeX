using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static void WriteStateCategoryHatchPattern(SvgMarkupWriter writer, string hatchId) {
        var spacing = ChartStateCategoryLegend.HatchSpacing;
        writer.StartElement("defs").EndStartElement()
            .StartElement("pattern").Attribute("id", hatchId).Attribute("width", spacing).Attribute("height", spacing).Attribute("patternUnits", "userSpaceOnUse").Attribute("patternTransform", "rotate(45)").EndStartElement()
            .StartElement("line").Attribute("x1", 0).Attribute("y1", 0).Attribute("x2", 0).Attribute("y2", spacing).Attribute("stroke", "#fff").Attribute("stroke-opacity", ChartStateCategoryLegend.HatchOpacity).Attribute("stroke-width", 1.5).EndEmptyElement()
            .EndElement().EndElement().Line();
    }

    private static void WriteStateCategoryHatch(SvgMarkupWriter writer, string hatchId, double x, double y, double width, double height, double radius = ChartStateCategoryLegend.SwatchRadius, string role = "state-segment-hatch") {
        writer.StartElement("rect").Attribute("data-cfx-role", role).Attribute("x", x).Attribute("y", y).Attribute("width", width).Attribute("height", height)
            .Attribute("rx", Math.Min(radius, width / 2)).Attribute("fill", "url(#" + hatchId + ")").Attribute("pointer-events", "none").EndEmptyElement().Line();
    }

    private static void WriteStateCategoryLegend(SvgMarkupWriter writer, Chart chart, IReadOnlyList<ChartStateCategoryLegendItem> legend, double top, string hatchId) {
        var style = chart.Options.LegendStyle;
        var fontSize = StyleFontSize(style, chart.Options.Theme.LegendFontSize);
        var swatch = ChartStateCategoryLegend.Swatch;
        foreach (var item in legend) {
            var rowY = top + item.Row * ChartStateCategoryLegend.RowHeight;
            writer.StartElement("rect").Attribute("data-cfx-role", "state-legend-swatch").Attribute("data-cfx-status", item.Category.Key).Attribute("x", item.X).Attribute("y", rowY)
                .Attribute("width", swatch).Attribute("height", swatch).Attribute("rx", ChartStateCategoryLegend.SwatchRadius).Attribute("fill", item.Category.Color.ToCss()).EndEmptyElement().Line();
            if (item.Category.Hatched) WriteStateCategoryHatch(writer, hatchId, item.X, rowY, swatch, swatch);
            WriteStateCategoryText(writer, chart, "state-legend-label", item.Category.Label, item.X + swatch + ChartStateCategoryLegend.LabelGap, rowY + swatch / 2, "start", fontSize, style, "400", true);
        }
    }

    private static void WriteStateCategoryText(SvgMarkupWriter writer, Chart chart, string role, string text, double x, double y, string anchor, double fontSize, TextStyleOverride style, string weight, bool middle, ChartColor? color = null) {
        if (text.Length == 0) return;
        writer.StartElement("text").Attribute("data-cfx-role", role).Attribute("x", x).Attribute("y", y).Attribute("text-anchor", anchor);
        if (middle) writer.Attribute("dominant-baseline", "middle");
        writer.Attribute("fill", StyleColor(style, color ?? chart.Options.Theme.MutedText).ToCss())
            .Attribute("font-family", SvgFontFamilyAttributeValue(StyleFontFamily(chart, style)))
            .Attribute("font-size", fontSize)
            .Attribute("font-weight", StyleWeight(style, weight));
        WriteSvgTextStyleAttributes(writer, style);
        WriteSvgStyledTextContent(writer, style, text).EndElement().Line();
    }
}
