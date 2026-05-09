using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Raster;

public sealed partial class PngChartRenderer {
    private static void DrawCircleChart(RgbaCanvas c, Chart chart, ChartRect plot) {
        ChartSeries? circle = null;
        foreach (var series in chart.Series) {
            if (series.Kind == ChartSeriesKind.Circle) {
                circle = series;
                break;
            }
        }

        if (circle == null || circle.Points.Count == 0) return;
        var min = circle.Points[0].X;
        var max = circle.Points.Count > 1 ? circle.Points[1].X : 100;
        if (Math.Abs(max - min) < 0.000001) max = min + 1;
        var value = Clamp(circle.Points[0].Y, min, max);
        var ratio = Clamp((value - min) / (max - min), 0, 1);
        var theme = chart.Options.Theme;
        var status = GaugeStatus(ratio);
        var statusColor = GaugeStatusColor(chart, status);
        var color = circle.Color ?? statusColor;
        var cx = plot.Left + plot.Width / 2;
        var cy = plot.Top + plot.Height * 0.54;
        var radius = Math.Max(24, Math.Min(plot.Width, plot.Height) * 0.28 * chart.Options.CircleRadiusScale);
        var stroke = Math.Max(5, radius * 0.16 * chart.Options.CircleStrokeScale);
        var start = -Math.PI / 2;
        c.DrawArc(cx, cy, radius, start, start + Math.PI * 2, ApplyOpacity(theme.Grid, ChartVisualPrimitives.CircleTrackOpacity), stroke);
        if (ratio > 0) {
            var end = start + Math.PI * 2 * ratio;
            c.DrawArc(cx, cy, radius, start, end, color, stroke);
        }

        var centerRadius = Math.Max(18, radius - stroke * 0.82);
        c.DrawCircle(cx, cy, centerRadius, ApplyOpacity(theme.CardBackground, ChartVisualPrimitives.CircleCenterFillOpacity));
        c.DrawCircleOutline(cx, cy, centerRadius, ApplyOpacity(theme.Grid, ChartVisualPrimitives.CircleCenterStrokeOpacity), 1);
        var valueLabel = FormatValue(chart, value);
        var labelWidth = Math.Max(60, Math.Min(plot.Width - 24, radius * 1.65));
        var valueFontSize = Math.Max(24, Math.Min(theme.TitleFontSize * 1.72, radius * 0.72));
        var titleFontSize = Math.Max(9, Math.Min(theme.LegendFontSize, radius * 0.22));
        if (circle.ShowDataLabels != false) {
            var centerLineGap = Math.Max(4, Math.Min(8, radius * 0.05));
            var valueHeight = EstimatePngTextHeight(valueFontSize);
            var titleHeight = EstimatePngTextHeight(titleFontSize);
            var groupTop = cy - (valueHeight + centerLineGap + titleHeight) / 2.0;
            DrawPngTextEmphasizedCenteredX(c, cx, groupTop, valueLabel, theme.Text, valueFontSize, labelWidth);
            DrawPngTextEmphasizedCenteredX(c, cx, groupTop + valueHeight + centerLineGap, circle.Name, theme.MutedText, titleFontSize, labelWidth);
            if (chart.Options.ShowCircleStatusLabel) {
                var statusLabel = status.Replace("-", " ");
                var statusFontSize = TextFontSizeForEmphasizedWidth(statusLabel, labelWidth, theme.TickLabelFontSize);
                statusLabel = TrimReadablePngLabelToWidth(statusLabel, statusFontSize, labelWidth);
                var statusLeft = cx - EstimatePngEmphasizedTextWidth(statusLabel, statusFontSize) / 2.0;
                c.DrawCircle(statusLeft - 9, cy + radius + 36, ChartVisualPrimitives.PngStatusMarkerOutlineRadius, theme.CardBackground);
                c.DrawCircle(statusLeft - 9, cy + radius + 36, ChartVisualPrimitives.StatusMarkerRadius, statusColor);
                c.DrawTextEmphasized(statusLeft, cy + radius + 40 - statusFontSize + 1, statusLabel, theme.MutedText, statusFontSize);
            }
        }
    }

    private static bool IsCircleChart(Chart chart) => ChartSeriesKindTraits.ContainsKind(chart, ChartSeriesKind.Circle);
}
