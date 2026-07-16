using System;
using System.Collections.Generic;
using System.Linq;
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

        var scale = RadialValueScale.Create(chart.Options.YAxis, series.Select(item => item.Series), "Radar");
        var tickFontSize = PngTickFontSize(chart);
        var cx = plot.Left + plot.Width / 2;
        var cy = plot.Top + plot.Height / 2 + 6;
        var radius = Math.Max(32, Math.Min(plot.Width, plot.Height) / 2 - 42);

        DrawRadarGrid(c, chart, categories, scale, cx, cy, radius, tickFontSize);
        for (var seriesOrder = 0; seriesOrder < series.Count; seriesOrder++) {
            var item = series[seriesOrder];
            var color = item.Series.Color ?? chart.Options.Theme.Palette[item.Index % chart.Options.Theme.Palette.Length];
            var points = RadarPoints(item.Series, categories, scale, cx, cy, radius);
            c.FillPolygon(points, ApplyOpacity(color, ChartVisualPrimitives.RadarAreaOpacity));
            for (var i = 0; i < points.Count; i++) {
                var next = points[(i + 1) % points.Count];
                c.DrawLine(points[i].X, points[i].Y, next.X, next.Y, color, ChartVisualPrimitives.RadarOutlineStrokeWidth);
                c.DrawCircle(points[i].X, points[i].Y, ChartVisualPrimitives.RadarPointRadius + ChartVisualPrimitives.PngRadarPointOutlineRadiusExtra, chart.Options.Theme.CardBackground);
                c.DrawCircle(points[i].X, points[i].Y, ChartVisualPrimitives.RadarPointRadius, color);
                if (ShouldDrawDataLabels(chart, item.Series)) {
                    var label = FormatValue(chart, RadarValue(item.Series, categories[i]));
                    var labelPoint = RadarDataLabelPoint(chart, item.Series, points[i], i, categories.Count, seriesOrder, series.Count);
                    var pointIndex = RadarPointIndex(item.Series, categories[i]);
                    var style = DataLabelStyle(chart, item.Series, pointIndex);
                    var fontSize = PngDataLabelFontSize(chart, item.Series, pointIndex);
                    DrawReadablePngLabel(c, plot, labelPoint.X - EstimatePngEmphasizedTextWidth(label, fontSize) / 2.0, labelPoint.Y - fontSize / 2.0, label, chart.Options.Theme.Text, ReadableLabelHalo(chart), fontSize, style);
                }
            }
        }

        DrawLegend(c, chart);
    }

    private static void DrawRadarGrid(RgbaCanvas c, Chart chart, IReadOnlyList<double> categories, RadialValueScale scale, double cx, double cy, double radius, double tickFontSize) {
        foreach (var tick in scale.Ticks) {
            if (tick <= scale.Minimum) continue;
            var ringRadius = radius * scale.Normalize(tick);
            var ring = RadarRing(categories.Count, cx, cy, ringRadius);
            if (chart.Options.ShowGrid) DrawRadarPolyline(c, ring, ApplyOpacity(chart.Options.Theme.Grid, ChartVisualPrimitives.RadarRingOpacity), ChartVisualPrimitives.GridStrokeWidth);
            var isOuterTick = scale.IsMaximum(tick);
            if (chart.Options.ShowAxes && chart.Options.YAxis.Visible && !isOuterTick) {
                var ringLabelMaxWidth = Math.Max(28, chart.Options.Size.Width - chart.Options.Padding.Right - cx - 14);
                var ringLabel = FormatYAxisValue(chart, tick);
                var ringFontSize = TextFontSizeForWidth(ringLabel, ringLabelMaxWidth, tickFontSize);
                ringLabel = TrimPngLabelToWidth(ringLabel, ringFontSize, ringLabelMaxWidth);
                if (ringLabel.Length > 0) c.DrawText(cx + 7, cy - ringRadius + 14 - ringFontSize + 1, ringLabel, chart.Options.Theme.MutedText, ringFontSize);
            }
        }

        for (var i = 0; i < categories.Count; i++) {
            var angle = RadarAngle(i, categories.Count);
            var endX = cx + Math.Cos(angle) * radius;
            var endY = cy + Math.Sin(angle) * radius;
            if (chart.Options.ShowGrid) c.DrawLine(cx, cy, endX, endY, ApplyOpacity(chart.Options.Theme.Grid, ChartVisualPrimitives.RadarSpokeOpacity), ChartVisualPrimitives.GridStrokeWidth);
            if (!chart.Options.ShowAxes || !chart.Options.XAxis.Visible) continue;
            var rawLabel = FormatX(chart, categories[i]);
            var maxWidth = Math.Max(44, RadarLabelWidth(chart, angle));
            var fontSize = TextFontSizeForEmphasizedWidth(rawLabel, maxWidth, tickFontSize);
            var label = TrimReadablePngLabelToWidth(rawLabel, fontSize, maxWidth);
            if (label.Length == 0) continue;
            var labelWidth = EstimatePngEmphasizedTextWidth(label, fontSize);
            var labelX = Clamp(endX + Math.Cos(angle) * (18 + fontSize * 0.35) - labelWidth / 2.0, chart.Options.Padding.Left + 2, chart.Options.Size.Width - chart.Options.Padding.Right - labelWidth - 2);
            var labelY = Clamp(endY + Math.Sin(angle) * (18 + fontSize * 0.35) - fontSize / 2, chart.Options.Padding.Top + 12, chart.Options.Size.Height - chart.Options.Padding.Bottom - 18);
            c.DrawTextEmphasized(labelX, labelY, label, chart.Options.Theme.MutedText, fontSize);
        }
    }

    private static double RadarLabelWidth(Chart chart, double angle) {
        var sideRoom = chart.Options.Size.Width * 0.18;
        return Math.Abs(Math.Cos(angle)) < 0.32 ? chart.Options.Size.Width * 0.26 : sideRoom;
    }

    private static void DrawRadarPolyline(RgbaCanvas c, IReadOnlyList<ChartPoint> points, ChartColor color, double thickness) {
        for (var i = 0; i < points.Count; i++) {
            var next = points[(i + 1) % points.Count];
            c.DrawLine(points[i].X, points[i].Y, next.X, next.Y, color, thickness);
        }
    }

    private static bool IsRadarChart(Chart chart) => ChartSeriesKindTraits.ContainsKind(chart, ChartSeriesKind.Radar);

    private static List<double> RadarCategories(IEnumerable<RadarSeriesItem> series) {
        var set = new SortedSet<double>();
        foreach (var item in series) foreach (var point in item.Series.Points) set.Add(point.X);
        return new List<double>(set);
    }

    private static List<ChartPoint> RadarPoints(ChartSeries series, IReadOnlyList<double> categories, RadialValueScale scale, double cx, double cy, double radius) {
        var points = new List<ChartPoint>(categories.Count);
        for (var i = 0; i < categories.Count; i++) {
            var value = RadarValue(series, categories[i]);
            var angle = RadarAngle(i, categories.Count);
            var r = radius * scale.Normalize(value);
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

    private static int RadarPointIndex(ChartSeries series, double category) {
        for (var i = 0; i < series.Points.Count; i++) if (Math.Abs(series.Points[i].X - category) < 0.000001) return i;
        return -1;
    }

    private static double RadarAngle(int index, int count) => -Math.PI / 2 + Math.PI * 2 * index / count;

    private static ChartPoint RadarDataLabelPoint(Chart chart, ChartSeries series, ChartPoint point, int categoryIndex, int categoryCount, int seriesOrder, int seriesCount) {
        var placement = DataLabelPlacement(chart, series);
        var angle = RadarAngle(categoryIndex, categoryCount);
        if (placement == ChartDataLabelPlacement.Center || placement == ChartDataLabelPlacement.Inside) return point;
        if (placement == ChartDataLabelPlacement.Left) return new ChartPoint(point.X - 20, point.Y);
        if (placement == ChartDataLabelPlacement.Right || placement == ChartDataLabelPlacement.Outside) return new ChartPoint(point.X + 20, point.Y);
        if (placement == ChartDataLabelPlacement.Above) return new ChartPoint(point.X, point.Y - 20);
        if (placement == ChartDataLabelPlacement.Below) return new ChartPoint(point.X, point.Y + 20);
        var inward = 16.0;
        var spread = (seriesOrder - (seriesCount - 1) / 2.0) * 18.0;
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
