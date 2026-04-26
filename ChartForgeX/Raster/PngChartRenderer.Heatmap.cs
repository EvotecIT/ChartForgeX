using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Raster;

public sealed partial class PngChartRenderer {
    private static void DrawHeatmap(RgbaCanvas c, Chart chart, ChartRect plot) {
        var rows = new List<ChartSeries>();
        foreach (var series in chart.Series) if (series.Kind == ChartSeriesKind.Heatmap) rows.Add(series);
        if (rows.Count == 0) return;

        var columns = new SortedSet<double>();
        var min = double.PositiveInfinity;
        var max = double.NegativeInfinity;
        foreach (var series in rows) {
            foreach (var point in series.Points) {
                columns.Add(point.X);
                if (point.Y < min) min = point.Y;
                if (point.Y > max) max = point.Y;
            }
        }

        if (columns.Count == 0) return;
        if (double.IsInfinity(min)) { min = 0; max = 1; }
        if (Math.Abs(max - min) < 0.000001) max = min + 1;

        var columnValues = new List<double>();
        foreach (var column in columns) columnValues.Add(column);
        var labelWidth = 0;
        foreach (var row in rows) labelWidth = Math.Max(labelWidth, EstimateTinyTextWidth(row.Name));
        plot = new ChartRect(plot.X + labelWidth + 12, plot.Y, Math.Max(1, plot.Width - labelWidth - 12), Math.Max(1, plot.Height - 42));
        var gap = Math.Min(4, Math.Max(1, Math.Min(plot.Width / columnValues.Count, plot.Height / rows.Count) * 0.04));
        var cellWidth = Math.Max(1, (plot.Width - gap * (columnValues.Count - 1)) / columnValues.Count);
        var cellHeight = Math.Max(1, (plot.Height - gap * (rows.Count - 1)) / rows.Count);

        for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++) {
            var series = rows[rowIndex];
            var y = plot.Top + rowIndex * (cellHeight + gap);
            c.DrawTextTiny(plot.Left - EstimateTinyTextWidth(series.Name) - 8, y + cellHeight / 2 - 4, series.Name, chart.Options.Theme.MutedText, 1);
            for (var columnIndex = 0; columnIndex < columnValues.Count; columnIndex++) {
                var value = FindHeatmapValue(series, columnValues[columnIndex]);
                var x = plot.Left + columnIndex * (cellWidth + gap);
                var color = HeatmapColor(chart, series.Color, value, min, max);
                c.FillRect(x, y, cellWidth, cellHeight, color);
            }
        }

        for (var columnIndex = 0; columnIndex < columnValues.Count; columnIndex++) {
            var label = FormatX(chart, columnValues[columnIndex]);
            var x = plot.Left + columnIndex * (cellWidth + gap) + cellWidth / 2 - EstimateTinyTextWidth(label) / 2.0;
            c.DrawTextTiny(x, plot.Bottom + 10, label, chart.Options.Theme.MutedText, 1);
        }
    }

    private static bool IsHeatmapChart(Chart chart) {
        foreach (var series in chart.Series) if (series.Kind == ChartSeriesKind.Heatmap) return true;
        return false;
    }

    private static ChartColor HeatmapColor(Chart chart, ChartColor? highColor, double value, double min, double max) {
        var ratio = HeatmapRatio(value, min, max);
        if (chart.Options.HeatmapScale == ChartHeatmapScale.Semantic) return SemanticHeatmapColor(chart, ratio);
        return Blend(chart.Options.Theme.PlotBackground, highColor ?? chart.Options.Theme.Palette[0], 0.18 + ratio * 0.82);
    }

    private static ChartColor SemanticHeatmapColor(Chart chart, double ratio) {
        var t = chart.Options.Theme;
        if (ratio < 0.60) return Blend(t.Negative, t.Warning, ratio / 0.60 * 0.42);
        if (ratio < 0.80) return Blend(t.Warning, t.Positive, (ratio - 0.60) / 0.20 * 0.5);
        return Blend(t.Warning, t.Positive, 0.65 + (ratio - 0.80) / 0.20 * 0.35);
    }

    private static double HeatmapRatio(double value, double min, double max) {
        if (min >= -0.000001 && max <= 100.000001) return Clamp(value / 100, 0, 1);
        return Clamp((value - min) / Math.Max(0.000001, max - min), 0, 1);
    }

    private static ChartColor Blend(ChartColor a, ChartColor b, double amount) {
        amount = Clamp(amount, 0, 1);
        return ChartColor.FromRgb(
            (byte)Math.Round(a.R + (b.R - a.R) * amount),
            (byte)Math.Round(a.G + (b.G - a.G) * amount),
            (byte)Math.Round(a.B + (b.B - a.B) * amount));
    }

    private static double FindHeatmapValue(ChartSeries series, double column) {
        foreach (var point in series.Points) {
            if (Math.Abs(point.X - column) < 0.000001) return point.Y;
        }

        return 0;
    }
}
