using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Raster;

public sealed partial class PngChartRenderer {
    private static void DrawRadialBar(RgbaCanvas c, Chart chart, ChartRect plot) {
        ChartSeries? series = null;
        foreach (var candidate in chart.Series) {
            if (candidate.Kind != ChartSeriesKind.RadialBar) continue;
            series = candidate;
            break;
        }

        if (series == null || series.Points.Count == 0) return;

        var count = series.Points.Count;
        var theme = chart.Options.Theme;
        var chartPlot = PngRadialBarPlot(chart, plot, series);
        var cx = chartPlot.Left + chartPlot.Width * 0.50;
        var cy = chartPlot.Top + chartPlot.Height * 0.50;
        var baseOuterRadius = Math.Min(chartPlot.Width * 0.36, chartPlot.Height * 0.38);
        var maxOuterRadius = Math.Min(chartPlot.Width * 0.44, chartPlot.Height * 0.44);
        var outerRadius = Math.Max(28, Math.Min(maxOuterRadius, baseOuterRadius * chart.Options.RadialBarRadiusScale));
        var gap = Math.Max(5, outerRadius * 0.035);
        var stroke = Math.Max(5, Math.Min(24, ((outerRadius - 18) / Math.Max(1, count) - gap) * chart.Options.RadialBarStrokeScale));
        if (stroke * count + gap * Math.Max(0, count - 1) > outerRadius - 12) stroke = Math.Max(6, (outerRadius - 12 - gap * Math.Max(0, count - 1)) / Math.Max(1, count));

        var start = -Math.PI / 2;
        var average = 0.0;
        foreach (var point in series.Points) average += point.Y;
        average /= count;

        for (var i = 0; i < count; i++) {
            var point = series.Points[i];
            var ratio = Clamp(point.Y / 100.0, 0, 1);
            var radius = outerRadius - i * (stroke + gap) - stroke / 2;
            if (radius <= stroke / 2) continue;
            var color = PngRadialBarColor(series, theme, i);
            c.DrawArc(cx, cy, radius, start, start + Math.PI * 2, ApplyOpacity(theme.Grid, ChartVisualPrimitives.RadialTrackOpacity), Math.Max(1, stroke));
            if (ratio <= 0) continue;
            var end = start + Math.PI * 2 * ratio;
            c.DrawArc(cx, cy, radius, start, end, color, Math.Max(1, stroke));
        }

        var centerLabel = FormatValue(chart, average);
        var labelWidth = Math.Max(54, Math.Min(chartPlot.Width * 0.32, outerRadius * 1.25));
        var centerDiskRadius = Math.Max(26, outerRadius - count * (stroke + gap) - 2);
        var valueFontSize = Math.Max(26, theme.TitleFontSize * 1.32);
        var nameFontSize = Math.Max(9, theme.LegendFontSize - 1);
        c.DrawCircle(cx, cy, centerDiskRadius, ApplyOpacity(theme.CardBackground, ChartVisualPrimitives.RadialCenterFillOpacity));
        c.DrawCircleOutline(cx, cy, centerDiskRadius, ApplyOpacity(theme.Grid, ChartVisualPrimitives.RadialCenterStrokeOpacity), 1);
        if (series.ShowDataLabels != false && chart.Options.ShowRadialBarCenterLabel) {
            DrawPngTextEmphasizedCenteredX(c, cx, cy - theme.TitleFontSize * 0.42 - valueFontSize / 2.0, centerLabel, theme.Text, valueFontSize, labelWidth);
            DrawPngTextEmphasizedCenteredX(c, cx, cy + theme.LegendFontSize + 14 - nameFontSize + 1, series.Name, theme.MutedText, nameFontSize, labelWidth);
        }
        if (chart.Options.ShowLegend) DrawRadialBarLegend(c, chart, plot, series);
    }

    private static void DrawRadialBarLegend(RgbaCanvas c, Chart chart, ChartRect plot, ChartSeries series) {
        var fontSize = PngLegendFontSize(chart);
        var area = PngRadialBarLegendArea(chart, plot, series);
        var rows = BuildPngRadialBarLegendRows(chart, series, area.Width);
        var y = PngRadialBarLegendStartY(chart, area, rows.Count);
        foreach (var row in rows) {
            if (y > area.Bottom) break;
            var x = PngRadialBarLegendRowX(chart, area, row.Width);
            foreach (var item in row.Items) {
                var itemX = x + item.X;
                var valueWidth = EstimatePngEmphasizedTextWidth(item.Value, fontSize);
                var labelMaxWidth = Math.Max(8, item.Width - valueWidth - ChartVisualPrimitives.RadialLegendMarkerRadius * 2 - 28);
                var labelFontSize = TextFontSizeForEmphasizedWidth(item.Label, labelMaxWidth, fontSize);
                var label = TrimReadablePngLabelToWidth(item.Label, labelFontSize, labelMaxWidth);
                c.DrawCircle(itemX, y - 4, ChartVisualPrimitives.RadialLegendMarkerRadius, item.Color);
                if (label.Length > 0) c.DrawTextEmphasized(itemX + 13, y - labelFontSize + 2, label, chart.Options.Theme.Text, labelFontSize);
                c.DrawTextEmphasized(itemX + item.Width - EstimatePngEmphasizedTextWidth(item.Value, fontSize) - 10, y - fontSize + 2, item.Value, chart.Options.Theme.MutedText, fontSize);
            }

            y += PngRadialBarLegendRowHeight(chart);
        }
    }

    private static ChartRect PngRadialBarPlot(Chart chart, ChartRect plot, ChartSeries series) {
        if (!chart.Options.ShowLegend) return plot;
        var reserve = PngRadialBarLegendReserve(chart, series, plot);
        if (PngIsLeftLegend(chart.Options.LegendPosition)) return new ChartRect(plot.X + reserve, plot.Y, Math.Max(1, plot.Width - reserve), plot.Height);
        if (PngIsRightLegend(chart.Options.LegendPosition)) return new ChartRect(plot.X, plot.Y, Math.Max(1, plot.Width - reserve), plot.Height);
        if (PngIsTopLegend(chart.Options.LegendPosition)) return new ChartRect(plot.X, plot.Y + reserve, plot.Width, Math.Max(1, plot.Height - reserve));
        if (PngIsBottomLegend(chart.Options.LegendPosition)) return new ChartRect(plot.X, plot.Y, plot.Width, Math.Max(1, plot.Height - reserve));
        return plot;
    }

    private static double PngRadialBarLegendReserve(Chart chart, ChartSeries series, ChartRect plot) {
        if (PngIsLeftLegend(chart.Options.LegendPosition) || PngIsRightLegend(chart.Options.LegendPosition)) return Math.Min(230, Math.Max(142, PngRadialBarLegendWidestItem(chart, series) + 22)) + ChartVisualPrimitives.SideLegendPlotGap;
        return 18 + BuildPngRadialBarLegendRows(chart, series, Math.Max(80, plot.Width - 80)).Count * PngRadialBarLegendRowHeight(chart) + ChartVisualPrimitives.LegendPlotGap;
    }

    private static double PngRadialBarLegendWidestItem(Chart chart, ChartSeries series) {
        var widest = 0.0;
        for (var i = 0; i < series.Points.Count; i++) {
            var label = SliceLabel(chart, series.Points[i], i);
            var value = FormatValue(chart, series.Points[i].Y);
            widest = Math.Max(widest, ChartVisualPrimitives.RadialLegendMarkerRadius * 2 + EstimatePngEmphasizedTextWidth(label, PngLegendFontSize(chart)) + EstimatePngEmphasizedTextWidth(value, PngLegendFontSize(chart)) + 34);
        }

        return widest;
    }

    private static ChartRect PngRadialBarLegendArea(Chart chart, ChartRect plot, ChartSeries series) {
        var reserve = PngRadialBarLegendReserve(chart, series, plot);
        if (PngIsLeftLegend(chart.Options.LegendPosition)) return new ChartRect(plot.Left + 18, plot.Top + 20, Math.Max(1, reserve - 28), Math.Max(1, plot.Height - 40));
        if (PngIsRightLegend(chart.Options.LegendPosition)) return new ChartRect(plot.Right - reserve + 10, plot.Top + 20, Math.Max(1, reserve - 28), Math.Max(1, plot.Height - 40));
        var y = PngIsTopLegend(chart.Options.LegendPosition) ? plot.Top + 14 : plot.Bottom - reserve - 4;
        return new ChartRect(plot.Left + 36, y, Math.Max(1, plot.Width - 72), reserve);
    }

    private static List<PngRadialBarLegendRow> BuildPngRadialBarLegendRows(Chart chart, ChartSeries series, double width) {
        var rows = new List<PngRadialBarLegendRow>();
        var row = new PngRadialBarLegendRow();
        rows.Add(row);
        var x = 0.0;
        var vertical = PngIsLeftLegend(chart.Options.LegendPosition) || PngIsRightLegend(chart.Options.LegendPosition);
        var maxX = Math.Max(48, width);
        for (var i = 0; i < series.Points.Count; i++) {
            var value = FormatValue(chart, series.Points[i].Y);
            var valueWidth = EstimatePngEmphasizedTextWidth(value, PngLegendFontSize(chart));
            var labelMax = Math.Max(24, maxX - valueWidth - ChartVisualPrimitives.RadialLegendMarkerRadius * 2 - 28);
            var rawLabel = SliceLabel(chart, series.Points[i], i);
            var labelFontSize = TextFontSizeForEmphasizedWidth(rawLabel, labelMax, PngLegendFontSize(chart));
            var label = TrimReadablePngLabelToWidth(rawLabel, labelFontSize, labelMax);
            var itemWidth = Math.Min(maxX, ChartVisualPrimitives.RadialLegendMarkerRadius * 2 + EstimatePngEmphasizedTextWidth(label, labelFontSize) + valueWidth + 34);
            if (row.Items.Count > 0 && (vertical || x + itemWidth > maxX)) {
                row = new PngRadialBarLegendRow();
                rows.Add(row);
                x = 0;
            }

            row.Items.Add(new PngRadialBarLegendItem(x, itemWidth, label, value, PngRadialBarColor(series, chart.Options.Theme, i)));
            row.Width = Math.Max(row.Width, x + itemWidth);
            x += itemWidth;
        }

        return rows;
    }

    private static double PngRadialBarLegendStartY(Chart chart, ChartRect area, int rows) =>
        PngIsBottomLegend(chart.Options.LegendPosition) ? area.Bottom - 18 - Math.Max(0, rows - 1) * PngRadialBarLegendRowHeight(chart) : area.Top + 16;

    private static double PngRadialBarLegendRowX(Chart chart, ChartRect area, double rowWidth) {
        if (chart.Options.LegendPosition == ChartLegendPosition.TopRight || chart.Options.LegendPosition == ChartLegendPosition.BottomRight || PngIsRightLegend(chart.Options.LegendPosition)) return area.Right - Math.Min(area.Width, rowWidth);
        if (chart.Options.LegendPosition == ChartLegendPosition.Top || chart.Options.LegendPosition == ChartLegendPosition.Bottom) return area.X + Math.Max(0, (area.Width - rowWidth) / 2.0);
        return area.X;
    }

    private static double PngRadialBarLegendRowHeight(Chart chart) => PngLegendFontSize(chart) + 10;

    private static ChartColor PngRadialBarColor(ChartSeries series, ChartForgeX.Themes.ChartTheme theme, int pointIndex) {
        if (pointIndex < series.PointColors.Count && series.PointColors[pointIndex].HasValue) return series.PointColors[pointIndex]!.Value;
        return series.Color ?? theme.Palette[pointIndex % theme.Palette.Length];
    }

    private static bool IsRadialBarChart(Chart chart) => ChartSeriesKindTraits.ContainsKind(chart, ChartSeriesKind.RadialBar);

    private sealed class PngRadialBarLegendRow {
        public List<PngRadialBarLegendItem> Items { get; } = new();
        public double Width { get; set; }
    }

    private readonly struct PngRadialBarLegendItem {
        public PngRadialBarLegendItem(double x, double width, string label, string value, ChartColor color) {
            X = x;
            Width = width;
            Label = label;
            Value = value;
            Color = color;
        }

        public double X { get; }
        public double Width { get; }
        public string Label { get; }
        public string Value { get; }
        public ChartColor Color { get; }
    }
}
