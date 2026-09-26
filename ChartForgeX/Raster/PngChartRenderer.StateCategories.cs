using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Raster;

public sealed partial class PngChartRenderer {
    private static void DrawStateCategoryLegend(RgbaCanvas c, Chart chart, IReadOnlyList<ChartStateCategoryLegendItem> legend, double top) {
        var style = chart.Options.LegendStyle;
        var fontSize = PngLegendFontSize(chart);
        var swatch = ChartStateCategoryLegend.Swatch;
        foreach (var item in legend) {
            var rowY = top + item.Row * ChartStateCategoryLegend.RowHeight;
            c.FillRoundedRect(item.X, rowY, swatch, swatch, ChartStateCategoryLegend.SwatchRadius, item.Category.Color);
            if (item.Category.Hatched) DrawStateCategoryHatch(c, item.X, rowY, swatch, swatch);
            DrawStateCategoryText(c, item.Category.Label, item.X + swatch + ChartStateCategoryLegend.LabelGap, rowY + swatch / 2, style, chart.Options.Theme.MutedText, fontSize, false);
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
