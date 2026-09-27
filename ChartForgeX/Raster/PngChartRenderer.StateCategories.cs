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
        var swatch = ChartStateCategoryLegend.Swatch;
        foreach (var item in legend) {
            var rowY = top + item.Row * LegendRowBudget.RowHeight(chart);
            var rowCenter = rowY + LegendRowBudget.RowHeight(chart) / 2;
            if (item.Omitted > 0) {
                DrawLegendOverflow(c, chart, new ChartRect(bounds.Left, rowY, bounds.Width, LegendRowBudget.RowHeight(chart)), rowCenter + EstimatePngStyledTextHeight(legendFontSize, legendStyle) / 2, item.Omitted);
                continue;
            }

            var state = item.Category!;
            var swatchY = rowCenter - ChartStateCategoryLegend.Swatch / 2;
            c.FillRoundedRect(item.X, swatchY, ChartStateCategoryLegend.Swatch, ChartStateCategoryLegend.Swatch, ChartStateCategoryLegend.SwatchRadius, state.Color);
            if (state.Hatched) DrawStateCategoryHatch(c, item.X, swatchY, ChartStateCategoryLegend.Swatch, ChartStateCategoryLegend.Swatch);
            var labelWidth = Math.Max(8, bounds.Right - item.X - ChartStateCategoryLegend.Swatch - 6);
            var label = TrimReadablePngLabelToWidth(state.Label, legendFontSize, labelWidth, legendStyle);
            DrawStateCategoryText(c, label, item.X + ChartStateCategoryLegend.Swatch + 6, rowCenter, legendStyle, t.MutedText, legendFontSize, false);
        }
    }

    private static void DrawStateCategoryHatch(RgbaCanvas c, double x, double y, double width, double height, double radius = ChartStateCategoryLegend.SwatchRadius) {
        var color = ApplyOpacity(ChartColor.White, ChartStateCategoryLegend.HatchOpacity);
        foreach (var line in ChartPatternLineGeometry.Build(ChartFillPattern.DiagonalForward, x, y, width, height, Math.Min(radius, width / 2), ChartStateCategoryLegend.HatchSpacing * 1.4142)) {
            c.DrawLine(line.X1, line.Y1, line.X2, line.Y2, color, 1.5);
        }
    }

    private static void DrawStateCategoryText(RgbaCanvas c, string text, double x, double centerY, TextStyleOverride style, ChartColor color, double fontSize, bool emphasized) {
        DrawPngTextStyled(c, x, centerY - EstimatePngStyledTextBoundsHeight(fontSize, style) / 2 - PngStyledTextTopExtent(fontSize, style), text, style, color, fontSize, emphasized);
    }
}
