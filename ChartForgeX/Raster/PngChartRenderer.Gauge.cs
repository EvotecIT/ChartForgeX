using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

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
        var cy = plot.Top + plot.Height * ChartVisualPrimitives.GaugeCenterYFactor;
        var radius = Math.Max(ChartVisualPrimitives.GaugeMinRadius, Math.Min(plot.Width * ChartVisualPrimitives.GaugeRadiusWidthFactor, plot.Height * ChartVisualPrimitives.GaugeRadiusHeightFactor));
        var stroke = Math.Max(ChartVisualPrimitives.GaugeMinStrokeWidth, radius * ChartVisualPrimitives.GaugeStrokeRadiusFactor);
        c.DrawArc(cx, cy, radius, Math.PI, Math.PI * 2, ApplyOpacity(chart.Options.Theme.Grid, ChartVisualPrimitives.GaugeTrackOpacity), stroke);
        c.DrawArc(cx, cy, radius, Math.PI, Math.PI + Math.PI * ratio, color, stroke);
        var label = FormatValue(chart, value);
        var theme = chart.Options.Theme;
        var valueFontSize = Math.Max(34, theme.TitleFontSize * 1.65);
        var nameFontSize = theme.LegendFontSize;
        var tickFontSize = theme.TickLabelFontSize;
        var statusLabel = status.Replace("-", " ");
        var labelWidth = Math.Max(48, Math.Min(plot.Width - 24, radius * 1.8));
        if (gauge.ShowDataLabels != false) {
            DrawPngTextEmphasizedCenteredX(c, cx, cy - radius * ChartVisualPrimitives.GaugeValueOffsetFactor - valueFontSize / 2.0, label, theme.Text, valueFontSize, labelWidth);
            DrawPngTextEmphasizedCenteredX(c, cx, cy + ChartVisualPrimitives.GaugeTitleOffsetY - nameFontSize + 1, gauge.Name, theme.MutedText, nameFontSize, labelWidth);
            var statusFontSize = TextFontSizeForEmphasizedWidth(statusLabel, labelWidth, tickFontSize);
            statusLabel = TrimReadablePngLabelToWidth(statusLabel, statusFontSize, labelWidth);
            var statusLeft = cx - EstimatePngEmphasizedTextWidth(statusLabel, statusFontSize) / 2.0;
            c.DrawCircle(statusLeft - ChartVisualPrimitives.GaugeStatusMarkerOffsetX, cy + ChartVisualPrimitives.GaugeStatusMarkerOffsetY, ChartVisualPrimitives.PngStatusMarkerOutlineRadius, theme.CardBackground);
            c.DrawCircle(statusLeft - ChartVisualPrimitives.GaugeStatusMarkerOffsetX, cy + ChartVisualPrimitives.GaugeStatusMarkerOffsetY, ChartVisualPrimitives.StatusMarkerRadius, statusColor);
            c.DrawTextEmphasized(statusLeft, cy + ChartVisualPrimitives.GaugeStatusTextOffsetY - statusFontSize + 1, statusLabel, theme.MutedText, statusFontSize);
        }
        if (chart.Options.ShowAxes) {
            var axisLabelWidth = Math.Max(32, radius * 0.76);
            DrawPngTextCentered(c, cx - radius, cy + ChartVisualPrimitives.GaugeAxisLabelOffsetY, FormatValue(chart, min), theme.MutedText, tickFontSize, axisLabelWidth);
            DrawPngTextCentered(c, cx + radius, cy + ChartVisualPrimitives.GaugeAxisLabelOffsetY, FormatValue(chart, max), theme.MutedText, tickFontSize, axisLabelWidth);
        }
    }

    private static void DrawPngTextCentered(RgbaCanvas c, double centerX, double baselineY, string text, ChartColor color, double preferredFontSize, double maxWidth) {
        var fontSize = TextFontSizeForWidth(text, maxWidth, preferredFontSize);
        text = TrimPngLabelToWidth(text, fontSize, maxWidth);
        if (text.Length == 0) return;
        c.DrawText(centerX - EstimatePngTextWidth(text, fontSize) / 2.0, baselineY - fontSize + 1, text, color, fontSize);
    }

    private static bool IsGaugeChart(Chart chart) => ChartSeriesKindTraits.ContainsKind(chart, ChartSeriesKind.Gauge);

    private static string GaugeStatus(double ratio) {
        if (ratio < 0.60) return "negative";
        if (ratio < 0.80) return "warning";
        return "positive";
    }

    private static ChartColor GaugeStatusColor(Chart chart, string status) {
        return status == "negative" ? chart.Options.Theme.Negative : status == "warning" ? chart.Options.Theme.Warning : chart.Options.Theme.Positive;
    }
}
