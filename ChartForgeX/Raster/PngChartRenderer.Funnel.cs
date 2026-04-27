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
        var metricsReserve = values.Count > 1 ? Math.Min(168, Math.Max(126, basePlot.Width * 0.22)) : 0;
        var innerLeft = basePlot.Left + 44;
        var innerRight = basePlot.Right - 44 - metricsReserve;
        var plot = new ChartRect(innerLeft, basePlot.Top + 18, Math.Max(120, innerRight - innerLeft), Math.Max(90, basePlot.Height - 62));
        var metricsX = Math.Min(basePlot.Right - metricsReserve + 10, plot.Right + 18);
        var gap = Math.Min(10, Math.Max(4, plot.Height / values.Count * 0.08));
        var segmentHeight = Math.Max(18, (plot.Height - gap * (values.Count - 1)) / values.Count);

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
            var segment = new[] {
                new ChartPoint(topLeft, y),
                new ChartPoint(topRight, y),
                new ChartPoint(bottomRight, y + segmentHeight),
                new ChartPoint(bottomLeft, y + segmentHeight)
            };
            c.FillPolygonVerticalGradient(segment, Blend(ChartColor.White, color, 0.86), Blend(ChartColor.Black, color, 0.92));
            DrawFunnelSegmentStroke(c, chart, segment);

            var label = FormatX(chart, values[i].X);
            var value = FormatValue(chart, values[i].Y);
            var centerX = plot.Left + plot.Width / 2;
            var centerY = y + segmentHeight / 2;
            var labelColor = FunnelTextColor(color);
            var labelFontSize = TextFontSizeForEmphasizedWidth(label, Math.Max(36, Math.Min(topWidth, bottomWidth) - 18), chart.Options.Theme.LegendFontSize);
            var valueFontSize = TextFontSizeForEmphasizedWidth(value, Math.Max(36, Math.Min(topWidth, bottomWidth) - 18), chart.Options.Theme.DataLabelFontSize);
            var labelY = centerY - labelFontSize - 2;
            var valueY = centerY + 4;
            var halo = FunnelTextHalo(labelColor, chart.Options.Theme.CardBackground);
            DrawReadablePngLabel(c, centerX - EstimatePngEmphasizedTextWidth(label, labelFontSize) / 2.0, labelY, label, labelColor, halo, labelFontSize);
            DrawReadablePngLabel(c, centerX - EstimatePngEmphasizedTextWidth(value, valueFontSize) / 2.0, valueY, value, labelColor, halo, valueFontSize);
            if (i > 0) {
                var retention = values[i].Y / values[0].Y;
                var dropOff = 1 - values[i].Y / values[i - 1].Y;
                var guideX = Math.Min(metricsX - 10, bottomRight + 8);
                var retentionLabel = FormatPercent(retention) + " retained";
                var dropOffLabel = "-" + FormatPercent(dropOff) + " from prev";
                var metricFontSize = Math.Min(
                    TextFontSizeForEmphasizedWidth(retentionLabel, Math.Max(32, basePlot.Right - metricsX - 6), PngTickFontSize(chart)),
                    TextFontSizeForEmphasizedWidth(dropOffLabel, Math.Max(32, basePlot.Right - metricsX - 6), PngTickFontSize(chart)));
                c.DrawDashedLine(guideX, y - gap * 0.35, guideX, y + segmentHeight * 0.45, chart.Options.Theme.Axis, 1, 3, 4);
                c.DrawTextEmphasized(metricsX, centerY - metricFontSize - 4, retentionLabel, chart.Options.Theme.MutedText, metricFontSize);
                c.DrawTextEmphasized(metricsX, centerY + 4, dropOffLabel, chart.Options.Theme.Negative, metricFontSize);
            }
        }
    }

    private static bool IsFunnelChart(Chart chart) {
        foreach (var series in chart.Series) if (series.Kind == ChartSeriesKind.Funnel) return true;
        return false;
    }

    private static double FunnelWidth(double plotWidth, double value, double max) {
        var ratio = max <= 0 ? 1 : Clamp(value / max, 0.04, 1);
        return Math.Max(70, plotWidth * (0.22 + ratio * 0.74));
    }

    private static void DrawFunnelSegmentStroke(RgbaCanvas c, Chart chart, IReadOnlyList<ChartPoint> segment) {
        var border = ApplyOpacity(chart.Options.Theme.CardBackground, 0.80);
        var highlight = ApplyOpacity(ChartColor.White, chart.Options.Theme.Background.R < 80 ? 0.14 : 0.22);
        for (var i = 0; i < segment.Count; i++) {
            var next = segment[(i + 1) % segment.Count];
            c.DrawLine(segment[i].X, segment[i].Y, next.X, next.Y, border, 1);
        }

        c.DrawLine(segment[0].X + 2, segment[0].Y + 1, segment[1].X - 2, segment[1].Y + 1, highlight, 1);
    }

    private static ChartColor FunnelTextColor(ChartColor background) {
        var luminance = (0.2126 * background.R + 0.7152 * background.G + 0.0722 * background.B) / 255.0;
        return luminance > 0.58 ? ChartColor.FromRgb(15, 23, 42) : ChartColor.White;
    }

    private static ChartColor FunnelTextHalo(ChartColor text, ChartColor cardBackground) =>
        text.R > 240 && text.G > 240 && text.B > 240 ? cardBackground : ChartColor.Transparent;
}
