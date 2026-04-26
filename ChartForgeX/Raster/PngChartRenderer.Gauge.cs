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
        var color = gauge.Color ?? GaugeStatusColor(chart, ratio);
        var cx = plot.Left + plot.Width / 2;
        var cy = plot.Top + plot.Height * 0.66;
        var radius = Math.Max(36, Math.Min(plot.Width * 0.36, plot.Height * 0.52));
        var stroke = Math.Max(8, (int)Math.Round(radius * 0.14));
        DrawGaugeArc(c, cx, cy, radius, Math.PI, Math.PI * 2, chart.Options.Theme.Grid, stroke);
        DrawGaugeArc(c, cx, cy, radius, Math.PI, Math.PI + Math.PI * ratio, color, stroke);
        var label = FormatValue(chart, value);
        c.DrawTextTiny(cx - EstimateTinyTextWidth(label), cy - 22, label, chart.Options.Theme.Text, 3);
        c.DrawTextTiny(cx - EstimateTinyTextWidth(gauge.Name) / 2.0, cy + 24, gauge.Name, chart.Options.Theme.MutedText, 1);
    }

    private static void DrawGaugeArc(RgbaCanvas c, double cx, double cy, double radius, double start, double end, ChartColor color, int stroke) {
        var steps = Math.Max(8, (int)Math.Ceiling((end - start) / Math.PI * 64));
        var previousX = cx + Math.Cos(start) * radius;
        var previousY = cy + Math.Sin(start) * radius;
        for (var i = 1; i <= steps; i++) {
            var angle = start + (end - start) * i / steps;
            var x = cx + Math.Cos(angle) * radius;
            var y = cy + Math.Sin(angle) * radius;
            c.DrawLine(previousX, previousY, x, y, color, stroke);
            previousX = x;
            previousY = y;
        }
    }

    private static bool IsGaugeChart(Chart chart) {
        foreach (var series in chart.Series) if (series.Kind == ChartSeriesKind.Gauge) return true;
        return false;
    }

    private static ChartColor GaugeStatusColor(Chart chart, double ratio) {
        if (ratio < 0.60) return chart.Options.Theme.Negative;
        if (ratio < 0.80) return chart.Options.Theme.Warning;
        return chart.Options.Theme.Positive;
    }
}
