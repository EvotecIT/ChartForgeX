using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    /// <summary>
    /// Defines the two line patterns state marks use: diagonal lines (<paramref name="hatchId"/>) and crossed diagonals
    /// (<paramref name="hatchId"/> plus <c>-cross</c>). Lines take the colour of the surface behind the marks, so they
    /// read as cuts in the mark on light and dark themes.
    /// </summary>
    private static void WriteStateCategoryHatchPattern(SvgMarkupWriter writer, string hatchId, Chart chart) {
        var spacing = ChartStateCategoryLegend.HatchSpacing;
        var stroke = SvgPaint.Of(ChartStateMark.Backdrop(chart), SvgColorRole.Surface);
        writer.StartElement("defs").EndStartElement()
            .StartElement("pattern").Attribute("id", hatchId).Attribute("width", spacing).Attribute("height", spacing).Attribute("patternUnits", "userSpaceOnUse").Attribute("patternTransform", "rotate(45)").EndStartElement()
            .StartElement("line").Attribute("x1", 0).Attribute("y1", 0).Attribute("x2", 0).Attribute("y2", spacing).Paint("stroke", stroke).Attribute("stroke-opacity", ChartStateCategoryLegend.HatchOpacity).Attribute("stroke-width", ChartStateMark.PatternLineWidth).EndEmptyElement()
            .EndElement()
            .StartElement("pattern").Attribute("id", hatchId + "-cross").Attribute("width", spacing).Attribute("height", spacing).Attribute("patternUnits", "userSpaceOnUse").Attribute("patternTransform", "rotate(45)").EndStartElement()
            .StartElement("line").Attribute("x1", 0).Attribute("y1", 0).Attribute("x2", 0).Attribute("y2", spacing).Paint("stroke", stroke).Attribute("stroke-opacity", ChartStateCategoryLegend.HatchOpacity).Attribute("stroke-width", ChartStateMark.PatternLineWidth).EndEmptyElement()
            .StartElement("line").Attribute("x1", 0).Attribute("y1", 0).Attribute("x2", spacing).Attribute("y2", 0).Paint("stroke", stroke).Attribute("stroke-opacity", ChartStateCategoryLegend.HatchOpacity).Attribute("stroke-width", ChartStateMark.PatternLineWidth).EndEmptyElement()
            .EndElement().EndElement().Line();
    }

    /// <summary>Writes the fill and fill strength of a state mark onto the element being written.</summary>
    private static SvgMarkupWriter WriteStateMarkFill(SvgMarkupWriter writer, ChartStateMark mark) {
        writer.Attribute("data-cfx-pattern", mark.PatternToken).Attribute("data-cfx-emphasis", mark.EmphasisToken).Paint("fill", SvgPaint.Of(mark.Color, SvgColorRole.Status));
        if (mark.FillOpacity < 0.999) writer.Attribute("fill-opacity", mark.FillOpacity);
        return writer;
    }

    /// <summary>
    /// Writes what a state mark draws over its fill: the line pattern of a hatched or cross-hatched mark, or the
    /// dashed outline of an outlined one. The outline is a separate rectangle inset by half its width, so it stays
    /// inside the mark as it does in the raster output. The outline role is <paramref name="role"/> with
    /// <c>-outline</c> in place of <c>-hatch</c>.
    /// </summary>
    private static void WriteStateMarkLines(SvgMarkupWriter writer, string hatchId, ChartStateMark mark, double x, double y, double width, double height, double radius = ChartStateCategoryLegend.SwatchRadius, string role = "state-segment-hatch") {
        if (mark.Outlined) {
            var inset = Math.Min(ChartStateMark.OutlineWidth / 2, Math.Min(width, height) / 2);
            writer.StartElement("rect").Attribute("data-cfx-role", role.Replace("-hatch", "-outline")).Attribute("x", x + inset).Attribute("y", y + inset)
                .Attribute("width", Math.Max(0, width - inset * 2)).Attribute("height", Math.Max(0, height - inset * 2)).Attribute("rx", Math.Max(0, Math.Min(radius, width / 2) - inset))
                .Attribute("fill", "none").Paint("stroke", SvgPaint.Of(mark.Color, SvgColorRole.Status)).Attribute("stroke-width", ChartStateMark.OutlineWidth)
                .Attribute("stroke-dasharray", SvgMarkupWriter.FormatNumber(ChartStateMark.OutlineDash) + " " + SvgMarkupWriter.FormatNumber(ChartStateMark.OutlineGap));
            if (mark.OutlineOpacity < 0.999) writer.Attribute("stroke-opacity", mark.OutlineOpacity);
            writer.Attribute("pointer-events", "none").EndEmptyElement().Line();
        }

        if (mark.Lines == ChartFillPattern.None) return;
        WriteStateCategoryHatch(writer, mark.Lines == ChartFillPattern.Crosshatch ? hatchId + "-cross" : hatchId, x, y, width, height, radius, role);
    }

    private static void WriteStateCategoryHatch(SvgMarkupWriter writer, string hatchId, double x, double y, double width, double height, double radius = ChartStateCategoryLegend.SwatchRadius, string role = "state-segment-hatch") {
        writer.StartElement("rect").Attribute("data-cfx-role", role).Attribute("x", x).Attribute("y", y).Attribute("width", width).Attribute("height", height)
            .Attribute("rx", Math.Min(radius, width / 2)).Attribute("fill", "url(#" + hatchId + ")").Attribute("pointer-events", "none").EndEmptyElement().Line();
    }

    private static void WriteStateCategoryLegend(SvgMarkupWriter writer, Chart chart, IReadOnlyList<ChartStateCategoryLegendItem> legend, double top, string hatchId, ChartRect bounds) {
        var style = chart.Options.LegendStyle;
        var legendStyle = style;
        var fontSize = StyleFontSize(style, chart.Options.Theme.LegendFontSize);
        var legendFontSize = fontSize;
        var t = chart.Options.Theme;
        foreach (var item in legend) {
            var rowY = top + item.Row * LegendRowBudget.RowHeight(chart);
            var rowCenter = rowY + LegendRowBudget.RowHeight(chart) / 2;
            if (item.Omitted > 0) {
                DrawLegendOverflow(writer, chart, new ChartRect(bounds.Left, rowY, bounds.Width, LegendRowBudget.RowHeight(chart)), rowCenter + EstimateSvgStyledTextHeight(legendFontSize, legendStyle) / 2, item.Omitted);
                continue;
            }

            var state = item.Category!;
            var swatchY = rowCenter - ChartStateCategoryLegend.Swatch / 2;
            var mark = ChartStateMark.For(chart, state);
            writer.StartElement("rect").Attribute("data-cfx-role", "state-legend-swatch").Attribute("data-cfx-status", state.Key).Attribute("x", item.X).Attribute("y", swatchY)
                .Attribute("width", ChartStateCategoryLegend.Swatch).Attribute("height", ChartStateCategoryLegend.Swatch).Attribute("rx", ChartStateCategoryLegend.SwatchRadius);
            WriteStateMarkFill(writer, mark).EndEmptyElement().Line();
            WriteStateMarkLines(writer, hatchId, mark, item.X, swatchY, ChartStateCategoryLegend.Swatch, ChartStateCategoryLegend.Swatch);
            var labelWidth = Math.Max(8, bounds.Right - item.X - ChartStateCategoryLegend.Swatch - 6);
            var label = TrimSvgLabelToWidth(chart, state.Label, legendFontSize, labelWidth, legendStyle);
            WriteStateCategoryText(writer, chart, "state-legend-label", label, item.X + ChartStateCategoryLegend.Swatch + 6, rowCenter, "start", legendFontSize, legendStyle, "400", true);
        }
    }

    /// <summary>
    /// Writes state-category text in <paramref name="color"/> (muted text by default) or, when given, the typed
    /// <paramref name="paint"/>; a colour set on <paramref name="style"/> wins over both.
    /// </summary>
    private static void WriteStateCategoryText(SvgMarkupWriter writer, Chart chart, string role, string text, double x, double y, string anchor, double fontSize, TextStyleOverride style, string weight, bool middle, ChartColor? color = null, SvgPaint? paint = null) {
        if (text.Length == 0) return;
        writer.StartElement("text").Attribute("data-cfx-role", role).Attribute("x", x).Attribute("y", y).Attribute("text-anchor", anchor);
        if (middle) writer.Attribute("dominant-baseline", "middle");
        writer.Paint("fill", style.Color is { } styleColor ? SvgPaint.Plain(styleColor) : paint ?? SvgPaint.Plain(color ?? chart.Options.Theme.MutedText))
            .Attribute("font-family", SvgFontFamilyAttributeValue(StyleFontFamily(chart, style)))
            .Attribute("font-size", fontSize)
            .Attribute("font-weight", StyleWeight(style, weight));
        WriteSvgTextStyleAttributes(writer, style);
        WriteSvgStyledTextContent(writer, style, text).EndElement().Line();
    }
}
