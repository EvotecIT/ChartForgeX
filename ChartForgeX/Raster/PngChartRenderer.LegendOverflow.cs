using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Raster;

public sealed partial class PngChartRenderer {
    private static void DrawLegendOverflow(RgbaCanvas canvas, Chart chart, ChartRect area, double y, int omitted) {
        var style = chart.Options.LegendStyle;
        var label = LegendRowBudget.Summary(omitted);
        var fontSize = TextFontSizeForEmphasizedWidth(label, Math.Max(8, area.Width), PngLegendFontSize(chart), style);
        var rowWidth = Math.Min(area.Width, EstimatePngStyledTextWidth(label, fontSize, style, emphasized: true));
        var x = PngLegendRowX(chart, area, rowWidth);
        DrawPngTextStyled(canvas, x, y - EstimatePngStyledTextHeight(fontSize, style) + 3, label, style, chart.Options.Theme.MutedText, fontSize, emphasized: true);
    }
}
