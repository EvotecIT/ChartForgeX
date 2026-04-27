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
        var tickFontSize = PngTickFontSize(chart);
        var dataFontSize = chart.Options.Theme.DataLabelFontSize;
        var rawLabelWidth = 0.0;
        foreach (var row in rows) rawLabelWidth = Math.Max(rawLabelWidth, EstimatePngEmphasizedTextWidth(row.Name, tickFontSize));
        var labelWidth = Math.Min(rawLabelWidth, Math.Max(0, plot.Width - 220));
        var bottomReserve = 56 + (string.IsNullOrWhiteSpace(chart.XAxisTitle) ? 0 : 20);
        var rowLabelMaxWidth = Math.Max(8, labelWidth);
        plot = new ChartRect(plot.X + labelWidth + 14, plot.Y, Math.Max(1, plot.Width - labelWidth - 14), Math.Max(1, plot.Height - bottomReserve));
        var gap = Math.Min(6, Math.Max(2, Math.Min(plot.Width / columnValues.Count, plot.Height / rows.Count) * 0.05));
        var cellWidth = Math.Max(1, (plot.Width - gap * (columnValues.Count - 1)) / columnValues.Count);
        var cellHeight = Math.Max(1, (plot.Height - gap * (rows.Count - 1)) / rows.Count);
        var radius = Math.Min(8, Math.Min(cellWidth, cellHeight) * 0.16);

        for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++) {
            var series = rows[rowIndex];
            var y = plot.Top + rowIndex * (cellHeight + gap);
            var rowLabel = TrimReadablePngLabelToWidth(series.Name, tickFontSize, rowLabelMaxWidth);
            c.DrawTextEmphasized(plot.Left - EstimatePngEmphasizedTextWidth(rowLabel, tickFontSize) - 10, y + cellHeight / 2 - tickFontSize / 2, rowLabel, chart.Options.Theme.MutedText, tickFontSize);
            for (var columnIndex = 0; columnIndex < columnValues.Count; columnIndex++) {
                var value = FindHeatmapValue(series, columnValues[columnIndex]);
                var x = plot.Left + columnIndex * (cellWidth + gap);
                var color = HeatmapColor(chart, series.Color, value, min, max);
                c.FillRoundedRect(x, y, cellWidth, cellHeight, radius, color);
                c.StrokeRoundedRect(x, y, cellWidth, cellHeight, radius, ApplyOpacity(chart.Options.Theme.CardBackground, 0.32));
                if (chart.Options.ShowDataLabels && cellWidth >= EstimatePngEmphasizedTextWidth("100%", dataFontSize) + 12 && cellHeight >= dataFontSize + 10) {
                    var label = FormatValue(chart, value);
                    DrawReadablePngLabelCentered(c, new ChartRect(x, y, cellWidth, cellHeight), label, HeatmapTextColor(color), color, dataFontSize);
                }
            }
        }

        for (var columnIndex = 0; columnIndex < columnValues.Count; columnIndex++) {
            var label = FormatX(chart, columnValues[columnIndex]);
            label = TrimReadablePngLabelToWidth(label, tickFontSize, Math.Max(8, cellWidth + gap));
            var width = EstimatePngEmphasizedTextWidth(label, tickFontSize);
            var x = Clamp(plot.Left + columnIndex * (cellWidth + gap) + cellWidth / 2 - width / 2.0, plot.Left + 2, plot.Right - width - 2);
            c.DrawTextEmphasized(x, plot.Bottom + 21 - tickFontSize + 1, label, chart.Options.Theme.MutedText, tickFontSize);
        }

        DrawDetailAxisTitles(c, chart, plot, DetailTextScale(chart));
        DrawHeatmapScale(c, chart, plot, min, max, rows[0].Color, tickFontSize);
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

    private static ChartColor HeatmapTextColor(ChartColor color) {
        var luminance = (0.2126 * color.R + 0.7152 * color.G + 0.0722 * color.B) / 255.0;
        return luminance > 0.54 ? ChartColor.FromRgb(15, 23, 42) : ChartColor.White;
    }

    private static void DrawHeatmapScale(RgbaCanvas c, Chart chart, ChartRect plot, double min, double max, ChartColor? highColor, double fontSize) {
        const int steps = 5;
        const double width = 118;
        const double height = 8;
        var x = plot.Right - width;
        var y = plot.Bottom + 32;
        var stepWidth = width / steps;

        for (var i = 0; i < steps; i++) {
            var ratio = i / (double)(steps - 1);
            var value = min + (max - min) * ratio;
            c.FillRoundedRect(x + i * stepWidth, y, stepWidth + 1, height, 2, HeatmapColor(chart, highColor, value, min, max));
        }

        var minLabel = FormatValue(chart, min);
        var maxLabel = FormatValue(chart, max);
        c.DrawText(x, y + 24 - fontSize + 1, minLabel, chart.Options.Theme.MutedText, fontSize);
        c.DrawText(x + width - EstimatePngTextWidth(maxLabel, fontSize), y + 24 - fontSize + 1, maxLabel, chart.Options.Theme.MutedText, fontSize);
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
