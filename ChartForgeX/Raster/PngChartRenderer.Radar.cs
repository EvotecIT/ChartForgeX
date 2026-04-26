using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Raster;

public sealed partial class PngChartRenderer {
    private static void DrawRadar(RgbaCanvas c, Chart chart, ChartRect plot) {
        var series = new List<RadarSeriesItem>();
        for (var i = 0; i < chart.Series.Count; i++) {
            if (chart.Series[i].Kind == ChartSeriesKind.Radar && chart.Series[i].Points.Count > 0) series.Add(new RadarSeriesItem(chart.Series[i], i));
        }

        if (series.Count == 0) return;
        var categories = RadarCategories(series);
        if (categories.Count < 3) return;

        var max = RadarMax(series);
        var ticks = ChartTicks.Generate(0, max, chart.Options.TickCount);
        foreach (var tick in ticks) if (tick > max) max = tick;
        var cx = plot.Left + plot.Width / 2;
        var cy = plot.Top + plot.Height / 2 + 6;
        var radius = Math.Max(24, Math.Min(plot.Width, plot.Height) / 2 - 38);

        DrawRadarGrid(c, chart, categories, ticks, max, cx, cy, radius);
        for (var seriesOrder = 0; seriesOrder < series.Count; seriesOrder++) {
            var item = series[seriesOrder];
            var color = item.Series.Color ?? chart.Options.Theme.Palette[item.Index % chart.Options.Theme.Palette.Length];
            var points = RadarPoints(item.Series, categories, max, cx, cy, radius);
            c.FillPolygon(points, ChartColor.FromRgba(color.R, color.G, color.B, 48));
            for (var i = 0; i < points.Count; i++) {
                var next = points[(i + 1) % points.Count];
                c.DrawLine(points[i].X, points[i].Y, next.X, next.Y, color, 2);
                c.DrawCircle(points[i].X, points[i].Y, 3.5, color);
                if (chart.Options.ShowDataLabels) {
                    var label = FormatValue(chart, RadarValue(item.Series, categories[i]));
                    var labelPoint = RadarDataLabelPoint(points[i], i, categories.Count, seriesOrder, series.Count);
                    c.DrawTextTiny(labelPoint.X - EstimateTinyTextWidth(label) / 2.0, labelPoint.Y - 4, label, chart.Options.Theme.Text, 1);
                }
            }
        }
    }

    private static void DrawRadarGrid(RgbaCanvas c, Chart chart, IReadOnlyList<double> categories, IReadOnlyList<double> ticks, double max, double cx, double cy, double radius) {
        foreach (var tick in ticks) {
            if (tick <= 0) continue;
            var ring = RadarRing(categories.Count, cx, cy, radius * tick / max);
            DrawRadarPolyline(c, ring, chart.Options.Theme.Grid, 1);
            if (chart.Options.ShowAxes) c.DrawTextTiny(cx + 6, cy - radius * tick / max + 8, FormatValue(chart, tick), chart.Options.Theme.MutedText, 1);
        }

        for (var i = 0; i < categories.Count; i++) {
            var angle = RadarAngle(i, categories.Count);
            var endX = cx + Math.Cos(angle) * radius;
            var endY = cy + Math.Sin(angle) * radius;
            c.DrawLine(cx, cy, endX, endY, chart.Options.Theme.Grid, 1);
            var label = FormatX(chart, categories[i]);
            c.DrawTextTiny(endX + Math.Cos(angle) * 18 - EstimateTinyTextWidth(label) / 2.0, endY + Math.Sin(angle) * 18 - 4, label, chart.Options.Theme.MutedText, 1);
        }
    }

    private static void DrawRadarPolyline(RgbaCanvas c, IReadOnlyList<ChartPoint> points, ChartColor color, int thickness) {
        for (var i = 0; i < points.Count; i++) {
            var next = points[(i + 1) % points.Count];
            c.DrawLine(points[i].X, points[i].Y, next.X, next.Y, color, thickness);
        }
    }

    private static bool IsRadarChart(Chart chart) {
        foreach (var series in chart.Series) if (series.Kind == ChartSeriesKind.Radar) return true;
        return false;
    }

    private static List<double> RadarCategories(IEnumerable<RadarSeriesItem> series) {
        var set = new SortedSet<double>();
        foreach (var item in series) foreach (var point in item.Series.Points) set.Add(point.X);
        return new List<double>(set);
    }

    private static double RadarMax(IEnumerable<RadarSeriesItem> series) {
        var max = 0.0;
        foreach (var item in series) foreach (var point in item.Series.Points) max = Math.Max(max, point.Y);
        return max <= 0 ? 1 : max;
    }

    private static List<ChartPoint> RadarPoints(ChartSeries series, IReadOnlyList<double> categories, double max, double cx, double cy, double radius) {
        var points = new List<ChartPoint>(categories.Count);
        for (var i = 0; i < categories.Count; i++) {
            var value = Clamp(RadarValue(series, categories[i]), 0, max);
            var angle = RadarAngle(i, categories.Count);
            var r = radius * value / max;
            points.Add(new ChartPoint(cx + Math.Cos(angle) * r, cy + Math.Sin(angle) * r));
        }

        return points;
    }

    private static List<ChartPoint> RadarRing(int count, double cx, double cy, double radius) {
        var points = new List<ChartPoint>(count);
        for (var i = 0; i < count; i++) {
            var angle = RadarAngle(i, count);
            points.Add(new ChartPoint(cx + Math.Cos(angle) * radius, cy + Math.Sin(angle) * radius));
        }

        return points;
    }

    private static double RadarValue(ChartSeries series, double category) {
        foreach (var point in series.Points) if (Math.Abs(point.X - category) < 0.000001) return point.Y;
        return 0;
    }

    private static double RadarAngle(int index, int count) => -Math.PI / 2 + Math.PI * 2 * index / count;

    private static ChartPoint RadarDataLabelPoint(ChartPoint point, int categoryIndex, int categoryCount, int seriesOrder, int seriesCount) {
        var angle = RadarAngle(categoryIndex, categoryCount);
        var inward = 14.0;
        var spread = (seriesOrder - (seriesCount - 1) / 2.0) * 16.0;
        var x = point.X - Math.Cos(angle) * inward - Math.Sin(angle) * spread;
        var y = point.Y - Math.Sin(angle) * inward + Math.Cos(angle) * spread;
        return new ChartPoint(x, y);
    }

    private readonly struct RadarSeriesItem {
        public RadarSeriesItem(ChartSeries series, int index) {
            Series = series;
            Index = index;
        }

        public ChartSeries Series { get; }

        public int Index { get; }
    }
}
