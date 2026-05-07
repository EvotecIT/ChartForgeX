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

        WriteRadarChartStart(sb);
        DrawRadarGrid(sb, chart, plot, categories, ticks, max, cx, cy, radius);
        for (var seriesOrder = 0; seriesOrder < seriesItems.Length; seriesOrder++) {
            var item = seriesItems[seriesOrder];
            var color = item.series.Color ?? chart.Options.Theme.Palette[item.index % chart.Options.Theme.Palette.Length];
            var points = RadarPoints(item.series, categories, max, cx, cy, radius);
            var summary = BuildRadarSummary(chart, item.series, categories);
            var path = RadarPath(points);
            WriteRadarArea(sb, item.index, summary, path, color.ToCss());
            WriteRadarOutline(sb, item.index, path, color.ToCss());
            for (var i = 0; i < points.Count; i++) {
                WriteRadarPoint(sb, item.index, i, points[i], color.ToCss(), t.CardBackground.ToCss());
                if (ShouldDrawDataLabels(chart, item.series)) {
                    var labelPoint = RadarDataLabelPoint(chart, item.series, points[i], i, categories.Length, seriesOrder, seriesItems.Length);
                    DrawDataLabel(sb, chart, FormatValue(chart, RadarValue(item.series, categories[i])), labelPoint.X, labelPoint.Y, plot, series: item.series, pointIndex: RadarPointIndex(item.series, categories[i]));
                }
            }
        }

        DrawLegend(sb, chart, chart.Options.Size.Width, chart.Options.Size.Height);
        WriteRadarChartEnd(sb);
    }

    private static void DrawRadarGrid(StringBuilder sb, Chart chart, ChartRect plot, IReadOnlyList<double> categories, IReadOnlyList<double> ticks, double max, double cx, double cy, double radius) {
        var t = chart.Options.Theme;
        foreach (var tick in ticks) {
            if (tick <= 0) continue;
            var ring = RadarRing(categories.Count, cx, cy, radius * tick / max);
            if (chart.Options.ShowGrid) WriteRadarRing(sb, RadarPath(ring), t.Grid.ToCss());
            var isOuterTick = Math.Abs(tick - max) <= Math.Max(0.000001, max * 0.000001);
            if (chart.Options.ShowAxes && !isOuterTick) {
                var label = FormatValue(chart, tick);
                DrawSvgTextLeft(sb, chart, "radar-ring-label", label, cx + 7, cy - radius * tick / max + 14, t.MutedText, t.TickLabelFontSize, Math.Max(28, plot.Right - cx - 14), "400");
            }
        }

        for (var i = 0; i < categories.Count; i++) {
            var angle = RadarAngle(i, categories.Count);
            var endX = cx + Math.Cos(angle) * radius;
            var endY = cy + Math.Sin(angle) * radius;
            if (chart.Options.ShowGrid) WriteRadarSpoke(sb, cx, cy, endX, endY, t.Grid.ToCss());
            if (chart.Options.ShowAxes) DrawRadarAxisLabel(sb, chart, plot, categories[i], cx + Math.Cos(angle) * (radius + 24), cy + Math.Sin(angle) * (radius + 24), angle);
        }
    }

    private static void DrawRadarAxisLabel(StringBuilder sb, Chart chart, ChartRect plot, double category, double x, double y, double angle) {
        var t = chart.Options.Theme;
        var rawLabel = FormatX(chart, category);
        var maxWidth = Math.Max(44, SvgRadarLabelWidth(chart, angle));
        var fontSize = TextFontSizeForSvgWidth(rawLabel, maxWidth, t.TickLabelFontSize);
        var label = TrimSvgLabelToWidth(rawLabel, fontSize, maxWidth);
        if (label.Length == 0) return;
        var labelBounds = new ChartRect(24, Math.Min(24, plot.Top), Math.Max(1, chart.Options.Size.Width - 48), Math.Max(1, chart.Options.Size.Height - 48));
        var safeX = EdgeAwareTextX(label, x, labelBounds, fontSize);
        var safeY = Clamp(y, labelBounds.Top + fontSize, labelBounds.Bottom - fontSize);
        var anchor = EdgeAwareAnchor(label, safeX, labelBounds, fontSize);
        if (anchor == "middle") anchor = Math.Cos(angle) > 0.32 ? "start" : Math.Cos(angle) < -0.32 ? "end" : "middle";
        WriteRadarAxisLabel(sb, label, safeX, safeY, anchor, t.MutedText.ToCss(), SvgFontFamilyAttributeValue(t.FontFamily), fontSize);
    }

    private static void WriteRadarChartStart(StringBuilder sb) {
        var writer = new SvgMarkupWriter(128);
        writer
            .StartElement("g")
            .Attribute("data-cfx-role", "radar-chart")
            .EndStartElement()
            .Line();
        sb.Append(writer.ToString());
    }

    private static void WriteRadarChartEnd(StringBuilder sb) {
        sb.AppendLine("</g>");
    }

    private static void WriteRadarArea(StringBuilder sb, int seriesIndex, string summary, string path, string fill) {
        var writer = new SvgMarkupWriter(512);
        writer
            .StartElement("path")
            .Attribute("data-cfx-role", "radar-area")
            .Attribute("data-cfx-series", seriesIndex)
            .Attribute("role", "img")
            .Attribute("aria-label", summary)
            .Attribute("d", path)
            .Attribute("fill", fill)
            .Attribute("opacity", ChartVisualPrimitives.RadarAreaOpacity)
            .EndEmptyElement()
            .Line();
        sb.Append(writer.Build());
    }

    private static void WriteRadarOutline(StringBuilder sb, int seriesIndex, string path, string stroke) {
        var writer = new SvgMarkupWriter(512);
        writer
            .StartElement("path")
            .Attribute("data-cfx-role", "radar-outline")
            .Attribute("data-cfx-series", seriesIndex)
            .Attribute("d", path)
            .Attribute("fill", "none")
            .Attribute("stroke", stroke)
            .Attribute("stroke-width", ChartVisualPrimitives.RadarOutlineStrokeWidth)
            .Attribute("stroke-linejoin", "round")
            .EndEmptyElement()
            .Line();
        sb.Append(writer.Build());
    }

    private static void WriteRadarPoint(StringBuilder sb, int seriesIndex, int pointIndex, ChartPoint point, string fill, string stroke) {
        var writer = new SvgMarkupWriter(384);
        writer
            .StartElement("circle")
            .Attribute("data-cfx-role", "radar-point")
            .Attribute("data-cfx-series", seriesIndex)
            .Attribute("data-cfx-point", pointIndex)
            .Attribute("cx", point.X)
            .Attribute("cy", point.Y)
            .Attribute("r", ChartVisualPrimitives.RadarPointRadius)
            .Attribute("fill", fill)
            .Attribute("stroke", stroke)
            .Attribute("stroke-width", ChartVisualPrimitives.RadarPointStrokeWidth)
            .EndEmptyElement()
            .Line();
        sb.Append(writer.Build());
    }

    private static void WriteRadarRing(StringBuilder sb, string path, string stroke) {
        var writer = new SvgMarkupWriter(384);
        writer
            .StartElement("path")
            .Attribute("data-cfx-role", "radar-ring")
            .Attribute("d", path)
            .Attribute("fill", "none")
            .Attribute("stroke", stroke)
            .Attribute("stroke-width", ChartVisualPrimitives.GridStrokeWidth)
            .Attribute("opacity", ChartVisualPrimitives.RadarRingOpacity)
            .EndEmptyElement()
            .Line();
        sb.Append(writer.Build());
    }

    private static void WriteRadarSpoke(StringBuilder sb, double cx, double cy, double endX, double endY, string stroke) {
        var writer = new SvgMarkupWriter(384);
        writer
            .StartElement("line")
            .Attribute("data-cfx-role", "radar-spoke")
            .Attribute("x1", cx)
            .Attribute("y1", cy)
            .Attribute("x2", endX)
            .Attribute("y2", endY)
            .Attribute("stroke", stroke)
            .Attribute("stroke-width", ChartVisualPrimitives.GridStrokeWidth)
            .Attribute("opacity", ChartVisualPrimitives.RadarSpokeOpacity)
            .EndEmptyElement()
            .Line();
        sb.Append(writer.Build());
    }

    private static void WriteRadarAxisLabel(StringBuilder sb, string label, double x, double y, string anchor, string fill, string fontFamily, double fontSize) {
        var writer = new SvgMarkupWriter(384);
        writer
            .StartElement("text")
            .Attribute("data-cfx-role", "radar-axis-label")
            .Attribute("x", x)
            .Attribute("y", y)
            .Attribute("text-anchor", anchor)
            .Attribute("dominant-baseline", "middle")
            .Attribute("fill", fill)
            .Attribute("font-family", fontFamily)
            .Attribute("font-size", fontSize)
            .Attribute("font-weight", "650")
            .Text(label)
            .EndElement()
            .Line();
        sb.Append(writer.Build());
    }

    private static double SvgRadarLabelWidth(Chart chart, double angle) {
        var sideRoom = chart.Options.Size.Width * 0.18;
        return Math.Abs(Math.Cos(angle)) < 0.32 ? chart.Options.Size.Width * 0.26 : sideRoom;
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

    private static int RadarPointIndex(ChartSeries series, double category) {
        for (var i = 0; i < series.Points.Count; i++) if (Math.Abs(series.Points[i].X - category) < 0.000001) return i;
        return -1;
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

    private static string RadarPath(IReadOnlyList<ChartPoint> points) {
        if (points.Count == 0) return string.Empty;
        var sb = new StringBuilder();
        sb.Append("M ").Append(F(points[0].X)).Append(' ').Append(F(points[0].Y));
        for (var i = 1; i < points.Count; i++) sb.Append(" L ").Append(F(points[i].X)).Append(' ').Append(F(points[i].Y));
        sb.Append(" Z");
        return sb.ToString();
    }
}
