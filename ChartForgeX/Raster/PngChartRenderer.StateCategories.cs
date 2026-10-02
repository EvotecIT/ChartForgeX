using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Raster;

public sealed partial class PngChartRenderer {
    private static void DrawStateCategoryLegend(RgbaCanvas c, Chart chart, IReadOnlyList<ChartStateCategoryLegendItem> legend, double top, ChartRect bounds) {
        var style = chart.Options.LegendStyle;
        var legendStyle = style;
        var fontSize = PngLegendFontSize(chart);
        var legendFontSize = fontSize;
        var t = chart.Options.Theme;
        foreach (var item in legend) {
            var rowY = top + item.Row * LegendRowBudget.RowHeight(chart);
            var rowCenter = rowY + LegendRowBudget.RowHeight(chart) / 2;
            if (item.Omitted > 0) {
                DrawLegendOverflow(c, chart, new ChartRect(bounds.Left, rowY, bounds.Width, LegendRowBudget.RowHeight(chart)), rowCenter + EstimatePngStyledTextHeight(legendFontSize, legendStyle) / 2, item.Omitted);
                continue;
            }

            var state = item.Category!;
            var swatchY = rowCenter - ChartStateCategoryLegend.Swatch / 2;
            DrawStateMark(c, ChartStateMark.For(chart, state), item.X, swatchY, ChartStateCategoryLegend.Swatch, ChartStateCategoryLegend.Swatch);
            var labelWidth = Math.Max(8, bounds.Right - item.X - ChartStateCategoryLegend.Swatch - 6);
            var label = TrimReadablePngLabelToWidth(state.Label, legendFontSize, labelWidth, legendStyle);
            DrawStateCategoryText(c, label, item.X + ChartStateCategoryLegend.Swatch + 6, rowCenter, legendStyle, t.MutedText, legendFontSize, false);
        }
    }

    /// <summary>Draws a state mark: its fill at the mark's strength, its line pattern, and its dashed outline.</summary>
    private static void DrawStateMark(RgbaCanvas c, ChartStateMark mark, double x, double y, double width, double height, double radius = ChartStateCategoryLegend.SwatchRadius) {
        c.FillRoundedRect(x, y, width, height, radius, ApplyOpacity(mark.Color, mark.FillOpacity));
        if (mark.Lines != ChartFillPattern.None) {
            var color = ApplyOpacity(mark.LineColor, ChartStateCategoryLegend.HatchOpacity);
            foreach (var line in ChartPatternLineGeometry.Build(mark.Lines, x, y, width, height, Math.Min(radius, width / 2), ChartStateCategoryLegend.HatchSpacing * 1.4142)) {
                c.DrawLine(line.X1, line.Y1, line.X2, line.Y2, color, ChartStateMark.PatternLineWidth);
            }
        }

        // The dashed outline sits inside the mark on the centre line the SVG outline rectangle uses.
        if (mark.Outlined) c.StrokeRoundedRectDashed(x, y, width, height, Math.Min(radius, width / 2), ApplyOpacity(mark.Color, mark.OutlineOpacity), ChartStateMark.OutlineWidth, ChartStateMark.OutlineDash, ChartStateMark.OutlineGap);
    }

    private static void DrawStateCategoryText(RgbaCanvas c, string text, double x, double centerY, TextStyleOverride style, ChartColor color, double fontSize, bool emphasized) {
        DrawPngTextStyled(c, x, centerY - EstimatePngStyledTextBoundsHeight(fontSize, style) / 2 - PngStyledTextTopExtent(fontSize, style), text, style, color, fontSize, emphasized);
    }
}
