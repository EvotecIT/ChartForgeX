using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Raster;

public sealed partial class PngChartRenderer {
    private static void DrawFunnel(RgbaCanvas c, Chart chart, ChartRect basePlot) {
        ChartSeries? series = null;
        foreach (var candidate in chart.Series) {
            if (candidate.Kind == ChartSeriesKind.Funnel) {
                series = candidate;
                break;
            }
        }

        if (series == null) return;
        var values = new List<ChartPoint>();
        foreach (var point in series.Points) if (point.Y > 0) values.Add(point);
        if (values.Count == 0) return;

        var max = 0.0;
        foreach (var point in values) max = Math.Max(max, point.Y);
        var plot = new ChartRect(basePlot.Left + 56, basePlot.Top + 18, Math.Max(100, basePlot.Width - 112), Math.Max(80, basePlot.Height - 62));
        var gap = Math.Min(8, Math.Max(4, plot.Height / values.Count * 0.08));
        var segmentHeight = Math.Max(16, (plot.Height - gap * (values.Count - 1)) / values.Count);

        for (var i = 0; i < values.Count; i++) {
            var y = plot.Top + i * (segmentHeight + gap);
            var topWidth = FunnelWidth(plot.Width, values[i].Y, max);
            var nextValue = i + 1 < values.Count ? values[i + 1].Y : values[i].Y * 0.82;
            var bottomWidth = FunnelWidth(plot.Width, nextValue, max);
            var topLeft = plot.Left + (plot.Width - topWidth) / 2;
            var topRight = topLeft + topWidth;
            var bottomLeft = plot.Left + (plot.Width - bottomWidth) / 2;
            var bottomRight = bottomLeft + bottomWidth;
            var color = series.Color ?? chart.Options.Theme.Palette[i % chart.Options.Theme.Palette.Length];
            c.FillPolygon(new[] {
                new ChartPoint(topLeft, y),
                new ChartPoint(topRight, y),
                new ChartPoint(bottomRight, y + segmentHeight),
                new ChartPoint(bottomLeft, y + segmentHeight)
            }, color);

            var label = FormatX(chart, values[i].X);
            var value = FormatValue(chart, values[i].Y);
            var centerX = plot.Left + plot.Width / 2;
            var centerY = y + segmentHeight / 2;
            c.DrawTextTiny(centerX - EstimateTinyTextWidth(label) / 2.0, centerY - 11, label, chart.Options.Theme.Text, 1);
            c.DrawTextTiny(centerX - EstimateTinyTextWidth(value) / 2.0, centerY + 5, value, chart.Options.Theme.Text, 1);
            if (i > 0) {
                var retention = values[i].Y / values[0].Y;
                var dropOff = 1 - values[i].Y / values[i - 1].Y;
                c.DrawTextTiny(plot.Right + 8, centerY - 12, FormatPercent(retention) + " retained", chart.Options.Theme.MutedText, 1);
                c.DrawTextTiny(plot.Right + 8, centerY + 4, "-" + FormatPercent(dropOff), chart.Options.Theme.Negative, 1);
            }
        }
    }

    private static bool IsFunnelChart(Chart chart) {
        foreach (var series in chart.Series) if (series.Kind == ChartSeriesKind.Funnel) return true;
        return false;
    }

    private static double FunnelWidth(double plotWidth, double value, double max) {
        var ratio = max <= 0 ? 1 : Clamp(value / max, 0.04, 1);
        return Math.Max(54, plotWidth * (0.22 + ratio * 0.74));
    }
}
