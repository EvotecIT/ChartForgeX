using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Raster;

public sealed partial class PngChartRenderer {
    private static void DrawLegend(RgbaCanvas c, Chart chart) {
        if (!chart.Options.ShowLegend || chart.Series.Count == 0) return;
        var theme = chart.Options.Theme;
        var fontSize = PngLegendFontSize(chart);
        var symbolWidth = 18;
        var rowHeight = fontSize + 6;
        var x = 40.0;
        var y = chart.Options.Size.Height - 34.0 - (PngLegendRowCount(chart) - 1) * rowHeight;
        var maxX = System.Math.Max(80, chart.Options.Size.Width - 40);

        for (var i = 0; i < chart.Series.Count; i++) {
            var label = PngLegendLabel(chart, i);
            var itemWidth = symbolWidth + 10 + EstimatePngEmphasizedTextWidth(label, fontSize) + 18;
            if (i > 0 && x + itemWidth > maxX) {
                x = 40;
                y += rowHeight;
            }

            if (y > chart.Options.Size.Height - 10) break;
            var color = chart.Series[i].Color ?? theme.Palette[i % theme.Palette.Length];
            DrawLegendSymbol(c, chart.Series[i].Kind, x, y - 5, color, theme.CardBackground);
            c.DrawTextEmphasized(x + symbolWidth + 8, y - fontSize + 3, label, theme.MutedText, fontSize);
            x += itemWidth;
        }
    }

    private static string PngLegendLabel(Chart chart, int index) =>
        TrimReadablePngLabelToWidth(chart.Series[index].Name, PngLegendFontSize(chart), PngLegendLabelMaxWidth(chart));

    private static double PngLegendLabelMaxWidth(Chart chart) => System.Math.Max(48, System.Math.Min(220, chart.Options.Size.Width * 0.34));

    private static void DrawLegendSymbol(RgbaCanvas c, ChartSeriesKind kind, double x, double y, ChartColor color, ChartColor background) {
        if (IsLineLikeLegend(kind)) {
            c.DrawLine(x, y, x + 18, y, color, 2);
            c.DrawCircle(x + 9, y, 4.2, background);
            c.DrawCircle(x + 9, y, 3.1, color);
        } else if (kind == ChartSeriesKind.Scatter) {
            c.DrawCircle(x + 9, y, 4.2, background);
            c.DrawCircle(x + 9, y, 3.4, color);
        } else {
            c.FillRoundedRect(x, y - 5, 10, 10, 2, color);
        }
    }
}
