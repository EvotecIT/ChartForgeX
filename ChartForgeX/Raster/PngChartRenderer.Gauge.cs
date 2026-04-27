using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Raster;

public sealed partial class PngChartRenderer {
    private static void DrawGauge(RgbaCanvas c, Chart chart, ChartRect plot) {
        ChartSeries? gauge = null;
        foreach (var series in chart.Series) {
            if (series.Kind == ChartSeriesKind.Gauge) {
                gauge = series;
                break;
            }
        }

        if (gauge == null || gauge.Points.Count == 0) return;
        var min = gauge.Points[0].X;
        var max = gauge.Points.Count > 1 ? gauge.Points[1].X : 100;
        if (Math.Abs(max - min) < 0.000001) max = min + 1;
        var value = Clamp(gauge.Points[0].Y, min, max);
        var ratio = Clamp((value - min) / (max - min), 0, 1);
        var status = GaugeStatus(ratio);
        var statusColor = GaugeStatusColor(chart, status);
        var color = gauge.Color ?? statusColor;
        var cx = plot.Left + plot.Width / 2;
        var cy = plot.Top + plot.Height * 0.66;
        var radius = Math.Max(48, Math.Min(plot.Width * 0.36, plot.Height * 0.52));
        var stroke = Math.Max(14, (int)Math.Round(radius * 0.16));
        c.DrawArc(cx, cy, radius, Math.PI, Math.PI * 2, chart.Options.Theme.Grid, stroke);
        c.DrawArc(cx, cy, radius, Math.PI, Math.PI + Math.PI * ratio, color, stroke);
        var label = FormatValue(chart, value);
        var theme = chart.Options.Theme;
        var valueFontSize = Math.Max(34, theme.TitleFontSize * 1.65);
        var nameFontSize = theme.LegendFontSize;
        var tickFontSize = theme.TickLabelFontSize;
        var statusLabel = status.Replace("-", " ");
        var labelWidth = Math.Max(48, Math.Min(plot.Width - 24, radius * 1.8));
        DrawPngTextEmphasizedCenteredX(c, cx, cy - radius * 0.1 - valueFontSize / 2.0, label, theme.Text, valueFontSize, labelWidth);
        DrawPngTextEmphasizedCenteredX(c, cx, cy + 34 - nameFontSize + 1, gauge.Name, theme.MutedText, nameFontSize, labelWidth);
        var statusFontSize = TextFontSizeForEmphasizedWidth(statusLabel, labelWidth, tickFontSize);
        statusLabel = TrimReadablePngLabelToWidth(statusLabel, statusFontSize, labelWidth);
        var statusLeft = cx - EstimatePngEmphasizedTextWidth(statusLabel, statusFontSize) / 2.0;
        c.DrawCircle(statusLeft - 9, cy + 53, 5.2, theme.CardBackground);
        c.DrawCircle(statusLeft - 9, cy + 53, 2.5, statusColor);
        c.DrawTextEmphasized(statusLeft, cy + 57 - statusFontSize + 1, statusLabel, theme.MutedText, statusFontSize);
        var minLabel = FormatValue(chart, min);
        var maxLabel = FormatValue(chart, max);
        c.DrawText(cx - radius - EstimatePngTextWidth(minLabel, tickFontSize) / 2.0, cy + 27 - tickFontSize + 1, minLabel, theme.MutedText, tickFontSize);
        c.DrawText(cx + radius - EstimatePngTextWidth(maxLabel, tickFontSize) / 2.0, cy + 27 - tickFontSize + 1, maxLabel, theme.MutedText, tickFontSize);
    }

    private static bool IsGaugeChart(Chart chart) {
        foreach (var series in chart.Series) if (series.Kind == ChartSeriesKind.Gauge) return true;
        return false;
    }

    private static string GaugeStatus(double ratio) {
        if (ratio < 0.60) return "negative";
        if (ratio < 0.80) return "warning";
        return "positive";
    }

    private static ChartColor GaugeStatusColor(Chart chart, string status) {
        return status == "negative" ? chart.Options.Theme.Negative : status == "warning" ? chart.Options.Theme.Warning : chart.Options.Theme.Positive;
    }
}
