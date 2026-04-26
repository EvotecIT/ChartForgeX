using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static void DrawLegend(StringBuilder sb, Chart chart, int w, int h) {
        if (!chart.Options.ShowLegend) return;
        var t = chart.Options.Theme;
        var rows = BuildLegendRows(chart, w);
        var y = h - 28.0 - Math.Max(0, rows.Count - 1) * LegendRowHeight;
        sb.AppendLine("<g data-cfx-role=\"legend\">");
        foreach (var row in rows) {
            sb.AppendLine($"<g data-cfx-role=\"legend-row\" transform=\"translate(0 {F(y)})\">");
            foreach (var item in row.Items) {
                var c = Color(chart, item.Index);
                sb.AppendLine($"<circle cx=\"{F(item.X)}\" cy=\"-4\" r=\"5\" fill=\"{c.ToCss()}\"/>");
                sb.AppendLine($"<text x=\"{F(item.X + 12)}\" y=\"0\" fill=\"{t.MutedText.ToCss()}\" font-family=\"{SvgFontFamily(t.FontFamily)}\" font-size=\"{F(t.LegendFontSize)}\" font-weight=\"600\">{Escape(chart.Series[item.Index].Name)}</text>");
            }

            sb.AppendLine("</g>");
            y += LegendRowHeight;
        }
        sb.AppendLine("</g>");
    }

    private static List<LegendRow> BuildLegendRows(Chart chart, int width) {
        var rows = new List<LegendRow>();
        if (chart.Series.Count == 0) return rows;

        var maxX = Math.Max(80, width - 40);
        var row = new LegendRow();
        rows.Add(row);
        var x = LegendStartX;
        for (var i = 0; i < chart.Series.Count; i++) {
            var itemWidth = 28 + EstimateTextWidth(chart.Series[i].Name, chart.Options.Theme.LegendFontSize) + 18;
            if (row.Items.Count > 0 && x + itemWidth > maxX) {
                row = new LegendRow();
                rows.Add(row);
                x = LegendStartX;
            }

            row.Items.Add(new LegendItem(i, x));
            x += itemWidth;
        }

        return rows;
    }

    private static void DrawLabelPill(StringBuilder sb, Chart chart, string label, double x, double y, ChartColor textColor, string anchor, ChartRect plot) {
        var t = chart.Options.Theme;
        var width = Math.Max(34, EstimateTextWidth(label, t.TickLabelFontSize) + 16);
        var placement = PlaceLabelPill(x, width, anchor, plot);
        var textX = placement.Anchor == "end" ? placement.X - 8 : placement.X + 8;
        sb.AppendLine($"<rect data-cfx-role=\"annotation-label\" x=\"{F(placement.RectX)}\" y=\"{F(y - 16)}\" width=\"{F(width)}\" height=\"22\" rx=\"6\" fill=\"{t.CardBackground.ToCss()}\" opacity=\"0.88\" stroke=\"{textColor.ToCss()}\" stroke-opacity=\"0.34\"/>");
        sb.AppendLine($"<text x=\"{F(textX)}\" y=\"{F(y)}\" text-anchor=\"{placement.Anchor}\" fill=\"{textColor.ToCss()}\" font-family=\"{SvgFontFamily(t.FontFamily)}\" font-size=\"{F(t.TickLabelFontSize)}\" font-weight=\"700\">{Escape(label)}</text>");
    }

    private static void DrawDataLabel(StringBuilder sb, Chart chart, string label, double x, double y, ChartRect plot, string role = "data-label") {
        var t = chart.Options.Theme;
        var fontSize = t.DataLabelFontSize;
        var safeY = Clamp(y, plot.Top + fontSize * 0.7, plot.Bottom - fontSize * 0.35);
        var anchor = EdgeAwareAnchor(label, x, plot, fontSize);
        var safeX = Clamp(x, plot.Left + 4, plot.Right - 4);
        sb.AppendLine($"<text data-cfx-role=\"{role}\" x=\"{F(safeX)}\" y=\"{F(safeY)}\" text-anchor=\"{anchor}\" dominant-baseline=\"middle\" fill=\"{t.Text.ToCss()}\" stroke=\"{t.CardBackground.ToCss()}\" stroke-width=\"3\" paint-order=\"stroke fill\" stroke-linejoin=\"round\" font-family=\"{SvgFontFamily(t.FontFamily)}\" font-size=\"{F(fontSize)}\" font-weight=\"700\">{Escape(label)}</text>");
    }

    private static void DrawHorizontalValueLabel(StringBuilder sb, Chart chart, string label, double x, double y, string anchor, ChartRect plot) {
        var t = chart.Options.Theme;
        var fontSize = t.DataLabelFontSize;
        var width = EstimateTextWidth(label, fontSize);
        var effectiveAnchor = anchor == "end" ? "end" : "start";
        var safeX = effectiveAnchor == "end"
            ? Clamp(x, plot.Left + width + 4, plot.Right - 4)
            : Clamp(x, plot.Left + 4, plot.Right - width - 4);
        if (safeX < plot.Left + 4) {
            effectiveAnchor = "start";
            safeX = plot.Left + 4;
        } else if (safeX > plot.Right - 4) {
            effectiveAnchor = "end";
            safeX = plot.Right - 4;
        }

        sb.AppendLine($"<text data-cfx-role=\"data-label\" x=\"{F(safeX)}\" y=\"{F(Clamp(y, plot.Top + 4, plot.Bottom - 4))}\" text-anchor=\"{effectiveAnchor}\" dominant-baseline=\"middle\" fill=\"{t.Text.ToCss()}\" stroke=\"{t.CardBackground.ToCss()}\" stroke-width=\"3\" paint-order=\"stroke fill\" stroke-linejoin=\"round\" font-family=\"{SvgFontFamily(t.FontFamily)}\" font-size=\"{F(fontSize)}\" font-weight=\"700\">{Escape(label)}</text>");
    }

    private static LabelPillPlacement PlaceLabelPill(double x, double width, string anchor, ChartRect plot) {
        var minX = plot.Left + 4;
        var maxX = plot.Right - 4;
        var effectiveAnchor = anchor == "end" ? "end" : "start";
        var effectiveX = Clamp(x, minX, maxX);
        var rectX = effectiveAnchor == "end" ? effectiveX - width : effectiveX;

        if (rectX < minX) {
            effectiveAnchor = "start";
            effectiveX = minX;
            rectX = effectiveX;
        }

        if (rectX + width > maxX) {
            effectiveAnchor = "end";
            effectiveX = maxX;
            rectX = effectiveX - width;
        }

        if (rectX < minX) rectX = minX;
        return new LabelPillPlacement(effectiveX, rectX, effectiveAnchor);
    }

    private static string EdgeAwareAnchor(string label, double x, ChartRect plot, double fontSize) {
        var halfWidth = EstimateTextWidth(label, fontSize) / 2;
        if (x - halfWidth < plot.Left) return "start";
        if (x + halfWidth > plot.Right) return "end";
        return "middle";
    }

    private static string RotatedAnchor(string label, double x, ChartRect plot, double angle, double fontSize) {
        var projectedWidth = EstimateTextWidth(label, fontSize) * Math.Abs(Math.Cos(angle * Math.PI / 180));
        if (x - projectedWidth < plot.Left) return "start";
        if (x + projectedWidth > plot.Right) return "end";
        return angle < 0 ? "end" : "start";
    }

    private static double EstimateTextWidth(string text, double fontSize) {
        var width = 0.0;
        foreach (var ch in text) width += char.IsWhiteSpace(ch) ? fontSize * 0.34 : char.IsUpper(ch) ? fontSize * 0.62 : fontSize * 0.54;
        return width;
    }

    private static ChartColor Color(Chart chart, int index) => chart.Series[index].Color ?? chart.Options.Theme.Palette[index % chart.Options.Theme.Palette.Length];

    private static string F(double v) => v.ToString("0.###", CultureInfo.InvariantCulture);

    private static string FormatNumber(double v) {
        var abs = Math.Abs(v);
        if (abs >= 1000000000) return (v / 1000000000).ToString("0.#", CultureInfo.InvariantCulture) + "B";
        if (abs >= 1000000) return (v / 1000000).ToString("0.#", CultureInfo.InvariantCulture) + "M";
        if (abs >= 1000) return (v / 1000).ToString("0.#", CultureInfo.InvariantCulture) + "k";
        return v.ToString("0.##", CultureInfo.InvariantCulture);
    }

    private static string FormatValue(Chart chart, double value) {
        var formatter = chart.Options.ValueFormatter;
        if (formatter == null) return FormatNumber(value);
        return formatter(value) ?? string.Empty;
    }

    private static string FormatPercent(double v) => v.ToString("0.#%", CultureInfo.InvariantCulture);

    private static string SvgFontFamily(string value) => Escape(string.IsNullOrWhiteSpace(value) ? "system-ui, sans-serif" : value);

    private static string Escape(string value) => value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");

    private static double Clamp(double value, double min, double max) => Math.Max(min, Math.Min(max, value));

    private static IReadOnlyList<double> GetXTicks(Chart chart, ChartRange range, ChartRect plot) {
        if (chart.Options.XAxisLabels.Count == 0) return ChartTicks.GenerateInside(range.MinX, range.MaxX, chart.Options.TickCount);
        var labels = chart.Options.XAxisLabels
            .Where(label => label.Value >= range.MinX && label.Value <= range.MaxX)
            .OrderBy(label => label.Value)
            .ToArray();
        if (chart.Options.XAxisLabelDensity == ChartLabelDensity.All || labels.Length < 3) return labels.Select(label => label.Value).ToArray();

        var widest = labels.Max(label => EstimateTextWidth(label.Text, chart.Options.Theme.TickLabelFontSize));
        var densityFactor = chart.Options.XAxisLabelDensity == ChartLabelDensity.Dense ? 0.72 : chart.Options.XAxisLabelDensity == ChartLabelDensity.Relaxed ? 1.35 : 1.0;
        var minSpacing = Math.Max(28, (widest + 18) * densityFactor);
        var maxCount = Math.Max(2, (int)Math.Floor(plot.Width / minSpacing) + 1);
        if (labels.Length <= maxCount) return labels.Select(label => label.Value).ToArray();

        var step = Math.Max(1, (int)Math.Ceiling((labels.Length - 1) / (double)(maxCount - 1)));
        var ticks = new List<double>();
        ticks.Add(labels[0].Value);
        for (var i = step; i < labels.Length - 1; i += step) {
            var distanceToLast = Math.Abs(ProjectX(labels[i].Value, range, plot) - ProjectX(labels[labels.Length - 1].Value, range, plot));
            if (distanceToLast >= minSpacing * 0.95) ticks.Add(labels[i].Value);
        }

        ticks.Add(labels[labels.Length - 1].Value);
        return ticks;
    }

    private static IReadOnlyList<double> GetHorizontalCategoryTicks(Chart chart, ChartRange range) {
        var categories = new SortedSet<double>();
        foreach (var series in chart.Series) {
            if (series.Kind != ChartSeriesKind.HorizontalBar) continue;
            foreach (var point in series.Points) {
                if (point.X >= range.MinY && point.X <= range.MaxY) categories.Add(point.X);
            }
        }

        if (categories.Count > 0) return categories.ToArray();
        return ChartTicks.GenerateInside(range.MinY, range.MaxY, chart.Options.TickCount);
    }

    private static double ProjectX(double value, ChartRange range, ChartRect plot) {
        var span = range.MaxX - range.MinX;
        if (Math.Abs(span) < 0.0000001) return plot.Left + plot.Width / 2;
        return plot.Left + (value - range.MinX) / span * plot.Width;
    }

    private static string FormatX(Chart chart, double value) {
        foreach (var label in chart.Options.XAxisLabels) {
            if (Math.Abs(label.Value - value) < 0.000001) return label.Text;
        }

        return FormatNumber(value);
    }

    private static string BuildLinePath(IReadOnlyList<ChartPoint> points, bool smooth) {
        if (points.Count == 0) return string.Empty;
        if (!smooth || points.Count < 3) {
            var path = new StringBuilder();
            path.Append("M ").Append(F(points[0].X)).Append(' ').Append(F(points[0].Y));
            for (var i = 1; i < points.Count; i++) path.Append(" L ").Append(F(points[i].X)).Append(' ').Append(F(points[i].Y));
            return path.ToString();
        }

        var sb = new StringBuilder();
        sb.Append("M ").Append(F(points[0].X)).Append(' ').Append(F(points[0].Y));
        for (var i = 0; i < points.Count - 1; i++) {
            var p0 = points[Math.Max(0, i - 1)];
            var p1 = points[i];
            var p2 = points[i + 1];
            var p3 = points[Math.Min(points.Count - 1, i + 2)];
            var c1x = p1.X + (p2.X - p0.X) / 6;
            var c1y = p1.Y + (p2.Y - p0.Y) / 6;
            var c2x = p2.X - (p3.X - p1.X) / 6;
            var c2y = p2.Y - (p3.Y - p1.Y) / 6;
            sb.Append(" C ").Append(F(c1x)).Append(' ').Append(F(c1y)).Append(' ')
                .Append(F(c2x)).Append(' ').Append(F(c2y)).Append(' ')
                .Append(F(p2.X)).Append(' ').Append(F(p2.Y));
        }

        return sb.ToString();
    }

    private static string BuildId(Chart chart) {
        unchecked {
            uint hash = 2166136261;
            Add(ref hash, chart.Title);
            Add(ref hash, chart.Subtitle);
            Add(ref hash, chart.Options.Size.Width.ToString(CultureInfo.InvariantCulture));
            Add(ref hash, chart.Options.Size.Height.ToString(CultureInfo.InvariantCulture));
            foreach (var series in chart.Series) {
                Add(ref hash, series.Name);
                Add(ref hash, series.Kind.ToString());
                foreach (var point in series.Points) {
                    Add(ref hash, point.X.ToString("R", CultureInfo.InvariantCulture));
                    Add(ref hash, point.Y.ToString("R", CultureInfo.InvariantCulture));
                }
            }

            return "cfx" + hash.ToString("x8", CultureInfo.InvariantCulture);
        }
    }

    private static void Add(ref uint hash, string value) {
        foreach (var ch in value) {
            hash ^= ch;
            hash *= 16777619;
        }
    }

    private static string BuildDescription(Chart chart) {
        var title = string.IsNullOrWhiteSpace(chart.Title) ? "Chart" : chart.Title;
        if (chart.Series.Count == 0) return title + " with no data series.";
        var names = string.Join(", ", chart.Series.Select(series => series.Name).ToArray());
        return title + " with " + chart.Series.Count.ToString(CultureInfo.InvariantCulture) + " data series: " + names + ".";
    }

    private static bool IsPieLike(Chart chart) => chart.Series.Count > 0 && (chart.Series[0].Kind == ChartSeriesKind.Pie || chart.Series[0].Kind == ChartSeriesKind.Donut);

    private static bool IsHorizontalBarChart(Chart chart) => chart.Series.Any(series => series.Kind == ChartSeriesKind.HorizontalBar);

    private static bool IsHeatmapChart(Chart chart) => chart.Series.Any(series => series.Kind == ChartSeriesKind.Heatmap);

    private static bool IsGaugeChart(Chart chart) => chart.Series.Any(series => series.Kind == ChartSeriesKind.Gauge);

    private static bool IsBulletChart(Chart chart) => chart.Series.Any(series => series.Kind == ChartSeriesKind.Bullet);

    private static bool IsWaterfallChart(Chart chart) => chart.Series.Any(series => series.Kind == ChartSeriesKind.Waterfall);

    private static bool IsRadarChart(Chart chart) => chart.Series.Any(series => series.Kind == ChartSeriesKind.Radar);

    private static bool IsFunnelChart(Chart chart) => chart.Series.Any(series => series.Kind == ChartSeriesKind.Funnel);

    private static bool IsTimelineChart(Chart chart) => chart.Series.Any(series => series.Kind == ChartSeriesKind.Timeline);

    private static string SliceLabel(Chart chart, ChartPoint point, int index) {
        foreach (var label in chart.Options.XAxisLabels) {
            if (Math.Abs(label.Value - point.X) < 0.000001) return label.Text;
        }

        return "Slice " + (index + 1).ToString(CultureInfo.InvariantCulture);
    }

    private sealed class LegendRow {
        public List<LegendItem> Items { get; } = new();
    }

    private readonly struct LegendItem {
        public LegendItem(int index, double x) {
            Index = index;
            X = x;
        }

        public int Index { get; }

        public double X { get; }
    }

    private readonly struct BarLayoutInfo {
        public BarLayoutInfo(double barWidth, double offset) {
            BarWidth = barWidth;
            Offset = offset;
        }

        public double BarWidth { get; }

        public double Offset { get; }
    }

    private readonly struct HorizontalBarLayoutInfo {
        public HorizontalBarLayoutInfo(double barHeight, double offset) {
            BarHeight = barHeight;
            Offset = offset;
        }

        public double BarHeight { get; }

        public double Offset { get; }
    }

    private readonly struct LabelPillPlacement {
        public LabelPillPlacement(double x, double rectX, string anchor) {
            X = x;
            RectX = rectX;
            Anchor = anchor;
        }

        public double X { get; }

        public double RectX { get; }

        public string Anchor { get; }
    }
}
