using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static void DrawRadar(StringBuilder sb, Chart chart, ChartRect plot, string id) {
        var seriesItems = chart.Series
            .Select((series, index) => new { series, index })
            .Where(item => item.series.Kind == ChartSeriesKind.Radar && item.series.Points.Count > 0)
            .ToArray();
        if (seriesItems.Length == 0) return;

        var categories = RadarCategories(seriesItems.Select(item => item.series));
        if (categories.Length < 3) return;

        var max = RadarMax(seriesItems.Select(item => item.series));
        var ticks = ChartTicks.Generate(0, max, chart.Options.TickCount).Where(tick => tick >= 0).ToArray();
        max = ticks.Length == 0 ? max : Math.Max(max, ticks[ticks.Length - 1]);
        var t = chart.Options.Theme;
        var cx = plot.Left + plot.Width / 2;
        var cy = plot.Top + plot.Height / 2 + 6;
        var radius = Math.Max(32, Math.Min(plot.Width, plot.Height) / 2 - 42);

        sb.AppendLine("<g data-cfx-role=\"radar-chart\">");
        DrawRadarGrid(sb, chart, plot, categories, ticks, max, cx, cy, radius);
        for (var seriesOrder = 0; seriesOrder < seriesItems.Length; seriesOrder++) {
            var item = seriesItems[seriesOrder];
            var color = item.series.Color ?? chart.Options.Theme.Palette[item.index % chart.Options.Theme.Palette.Length];
            var points = RadarPoints(item.series, categories, max, cx, cy, radius);
            var summary = BuildRadarSummary(chart, item.series, categories);
            sb.AppendLine($"<path data-cfx-role=\"radar-area\" data-cfx-series=\"{item.index}\" role=\"img\" aria-label=\"{Escape(summary)}\" d=\"{RadarPath(points)}\" fill=\"{color.ToCss()}\" opacity=\"0.20\"/>");
            sb.AppendLine($"<path data-cfx-role=\"radar-outline\" data-cfx-series=\"{item.index}\" d=\"{RadarPath(points)}\" fill=\"none\" stroke=\"{color.ToCss()}\" stroke-width=\"2.4\" stroke-linejoin=\"round\"/>");
            for (var i = 0; i < points.Count; i++) {
                sb.AppendLine($"<circle data-cfx-role=\"radar-point\" data-cfx-series=\"{item.index}\" data-cfx-point=\"{i}\" cx=\"{F(points[i].X)}\" cy=\"{F(points[i].Y)}\" r=\"3.8\" fill=\"{color.ToCss()}\" stroke=\"{t.CardBackground.ToCss()}\" stroke-width=\"1.4\"/>");
                if (chart.Options.ShowDataLabels) {
                    var labelPoint = RadarDataLabelPoint(points[i], i, categories.Length, seriesOrder, seriesItems.Length);
                    DrawDataLabel(sb, chart, FormatValue(chart, RadarValue(item.series, categories[i])), labelPoint.X, labelPoint.Y, plot);
                }
            }
        }

        DrawLegend(sb, chart, chart.Options.Size.Width, chart.Options.Size.Height);
        sb.AppendLine("</g>");
    }

    private static void DrawRadarGrid(StringBuilder sb, Chart chart, ChartRect plot, IReadOnlyList<double> categories, IReadOnlyList<double> ticks, double max, double cx, double cy, double radius) {
        var t = chart.Options.Theme;
        foreach (var tick in ticks) {
            if (tick <= 0) continue;
            var ring = RadarRing(categories.Count, cx, cy, radius * tick / max);
            sb.AppendLine($"<path data-cfx-role=\"radar-ring\" d=\"{RadarPath(ring)}\" fill=\"none\" stroke=\"{t.Grid.ToCss()}\" stroke-width=\"1\" opacity=\"0.76\"/>");
            var isOuterTick = Math.Abs(tick - max) <= Math.Max(0.000001, max * 0.000001);
            if (chart.Options.ShowAxes && !isOuterTick) sb.AppendLine($"<text data-cfx-role=\"radar-ring-label\" x=\"{F(cx + 7)}\" y=\"{F(cy - radius * tick / max + 14)}\" fill=\"{t.MutedText.ToCss()}\" font-family=\"{SvgFontFamily(t.FontFamily)}\" font-size=\"{F(t.TickLabelFontSize)}\">{Escape(FormatValue(chart, tick))}</text>");
        }

        for (var i = 0; i < categories.Count; i++) {
            var angle = RadarAngle(i, categories.Count);
            var endX = cx + Math.Cos(angle) * radius;
            var endY = cy + Math.Sin(angle) * radius;
            sb.AppendLine($"<line data-cfx-role=\"radar-spoke\" x1=\"{F(cx)}\" y1=\"{F(cy)}\" x2=\"{F(endX)}\" y2=\"{F(endY)}\" stroke=\"{t.Grid.ToCss()}\" stroke-width=\"1\" opacity=\"0.72\"/>");
            DrawRadarAxisLabel(sb, chart, plot, categories[i], cx + Math.Cos(angle) * (radius + 24), cy + Math.Sin(angle) * (radius + 24), angle);
        }
    }

    private static void DrawRadarAxisLabel(StringBuilder sb, Chart chart, ChartRect plot, double category, double x, double y, double angle) {
        var t = chart.Options.Theme;
        var label = FormatX(chart, category);
        var labelBounds = new ChartRect(24, Math.Min(24, plot.Top), Math.Max(1, chart.Options.Size.Width - 48), Math.Max(1, chart.Options.Size.Height - 48));
        var safeX = EdgeAwareTextX(label, x, labelBounds, t.TickLabelFontSize);
        var safeY = Clamp(y, labelBounds.Top + t.TickLabelFontSize, labelBounds.Bottom - t.TickLabelFontSize);
        var anchor = EdgeAwareAnchor(label, safeX, labelBounds, t.TickLabelFontSize);
        if (anchor == "middle") anchor = Math.Cos(angle) > 0.32 ? "start" : Math.Cos(angle) < -0.32 ? "end" : "middle";
        sb.AppendLine($"<text data-cfx-role=\"radar-axis-label\" x=\"{F(safeX)}\" y=\"{F(safeY)}\" text-anchor=\"{anchor}\" dominant-baseline=\"middle\" fill=\"{t.MutedText.ToCss()}\" font-family=\"{SvgFontFamily(t.FontFamily)}\" font-size=\"{F(t.TickLabelFontSize)}\" font-weight=\"650\">{Escape(label)}</text>");
    }

    private static double[] RadarCategories(IEnumerable<ChartSeries> series) {
        var categories = new SortedSet<double>();
        foreach (var item in series) foreach (var point in item.Points) categories.Add(point.X);
        return categories.ToArray();
    }

    private static double RadarMax(IEnumerable<ChartSeries> series) {
        var max = 0.0;
        foreach (var item in series) foreach (var point in item.Points) max = Math.Max(max, point.Y);
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

    private static string BuildRadarSummary(Chart chart, ChartSeries series, IReadOnlyList<double> categories) {
        var sb = new StringBuilder(series.Name);
        sb.Append(": ");
        for (var i = 0; i < categories.Count; i++) {
            if (i > 0) sb.Append(", ");
            sb.Append(FormatX(chart, categories[i])).Append(' ').Append(FormatValue(chart, RadarValue(series, categories[i])));
        }

        return sb.ToString();
    }

    private static ChartPoint RadarDataLabelPoint(ChartPoint point, int categoryIndex, int categoryCount, int seriesOrder, int seriesCount) {
        var angle = RadarAngle(categoryIndex, categoryCount);
        var inward = 16.0;
        var spread = (seriesOrder - (seriesCount - 1) / 2.0) * 18.0;
        var x = point.X - Math.Cos(angle) * inward - Math.Sin(angle) * spread;
        var y = point.Y - Math.Sin(angle) * inward + Math.Cos(angle) * spread;
        return new ChartPoint(x, y);
    }

    private static string RadarPath(IReadOnlyList<ChartPoint> points) {
        if (points.Count == 0) return string.Empty;
        var sb = new StringBuilder();
        sb.Append("M ").Append(F(points[0].X)).Append(' ').Append(F(points[0].Y));
        for (var i = 1; i < points.Count; i++) sb.Append(" L ").Append(F(points[i].X)).Append(' ').Append(F(points[i].Y));
        sb.Append(" Z");
        return sb.ToString();
    }
}
