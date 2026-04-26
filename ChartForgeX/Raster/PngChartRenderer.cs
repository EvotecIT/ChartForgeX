using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Raster;

/// <summary>
/// Renders charts to dependency-free PNG images.
/// </summary>
public sealed partial class PngChartRenderer {
    /// <summary>
    /// Renders the specified chart to PNG bytes.
    /// </summary>
    /// <param name="chart">The chart to render.</param>
    /// <returns>A PNG image.</returns>
    public byte[] Render(Chart chart) {
        var o = chart.Options; var t = o.Theme;
        var c = new RgbaCanvas(o.Size.Width, o.Size.Height);
        c.Clear(o.TransparentBackground ? ChartColor.Transparent : t.Background);
        if (o.ShowCard && t.UseCard) c.FillRect(14, 14, o.Size.Width - 28, o.Size.Height - 28, t.CardBackground);
        var plot = ChartLayout.PlotArea(o);
        if (o.ShowPlotBackground) c.FillRect(plot.X, plot.Y, plot.Width, plot.Height, t.PlotBackground);
        if (o.ShowHeader) c.DrawTextTiny(40, 38, chart.Title, t.Text, 3);
        if (IsPieLike(chart)) {
            DrawPieLike(c, chart, plot);
            return PngWriter.WriteRgba(c.Width, c.Height, c.Pixels);
        }
        if (IsGaugeChart(chart)) {
            DrawGauge(c, chart, plot);
            return PngWriter.WriteRgba(c.Width, c.Height, c.Pixels);
        }
        if (IsBulletChart(chart)) {
            DrawBullet(c, chart, plot);
            return PngWriter.WriteRgba(c.Width, c.Height, c.Pixels);
        }
        if (IsWaterfallChart(chart)) {
            DrawWaterfall(c, chart, plot);
            return PngWriter.WriteRgba(c.Width, c.Height, c.Pixels);
        }
        if (IsRadarChart(chart)) {
            DrawRadar(c, chart, plot);
            return PngWriter.WriteRgba(c.Width, c.Height, c.Pixels);
        }
        if (IsFunnelChart(chart)) {
            DrawFunnel(c, chart, plot);
            return PngWriter.WriteRgba(c.Width, c.Height, c.Pixels);
        }
        if (IsHeatmapChart(chart)) {
            DrawHeatmap(c, chart, plot);
            return PngWriter.WriteRgba(c.Width, c.Height, c.Pixels);
        }
        if (IsTimelineChart(chart)) {
            DrawTimeline(c, chart, plot);
            return PngWriter.WriteRgba(c.Width, c.Height, c.Pixels);
        }
        var range = ChartRange.FromChart(chart);
        IReadOnlyList<double> yTicks;
        IReadOnlyList<double> xTicks;
        if (IsHorizontalBarChart(chart)) {
            xTicks = ChartTicks.Generate(range.MinX, range.MaxX, o.TickCount);
            ApplyHorizontalValueBounds(chart, range, xTicks);
            yTicks = GetHorizontalCategoryTicks(chart, range);
            plot = ApplyHorizontalBarReserve(chart, plot, yTicks);
            if (o.ShowPlotBackground) c.FillRect(plot.X, plot.Y, plot.Width, plot.Height, t.PlotBackground);
        } else {
            yTicks = ChartTicks.Generate(range.MinY, range.MaxY, o.TickCount);
            xTicks = GetXTicks(chart, range);
            range.SetYBounds(yTicks[0], yTicks[yTicks.Count - 1]);
        }

        var map = new ChartMapper(plot, range);
        if (IsHorizontalBarChart(chart)) {
            DrawHorizontalBarGrid(c, chart, plot, map, xTicks, yTicks);
            for (var i = 0; i < chart.Series.Count; i++) DrawSeries(c, chart, i, plot, map);
            return PngWriter.WriteRgba(c.Width, c.Height, c.Pixels);
        }

        DrawAnnotationBands(c, chart, plot, map);
        foreach (var yv in yTicks) {
            var y = map.Y(yv);
            if (o.ShowGrid) c.DrawLine(plot.Left, y, plot.Right, y, t.Grid, 1);
            if (o.ShowAxes) c.DrawTextTiny(12, y - 5, FormatValue(chart, yv), t.MutedText, 1);
        }
        foreach (var xv in xTicks) {
            var x = map.X(xv);
            if (o.ShowGrid) c.DrawLine(x, plot.Top, x, plot.Bottom, ChartColor.FromRgba(t.Grid.R, t.Grid.G, t.Grid.B, (byte)(t.Grid.A / 2)), 1);
            if (o.ShowAxes) c.DrawTextTiny(x - EstimateTinyTextWidth(FormatX(chart, xv)) / 2.0, plot.Bottom + 10, FormatX(chart, xv), t.MutedText, 1);
        }
        if (o.ShowAxes) {
            var zeroY = map.Y(0);
            if (zeroY > plot.Top && zeroY < plot.Bottom) c.DrawLine(plot.Left, zeroY, plot.Right, zeroY, t.Axis, 1);
            c.DrawLine(plot.Left, plot.Bottom, plot.Right, plot.Bottom, t.Axis, 1);
            c.DrawLine(plot.Left, plot.Top, plot.Left, plot.Bottom, t.Axis, 1);
        }
        DrawAnnotationLines(c, chart, plot, map);
        for (var i = 0; i < chart.Series.Count; i++) DrawSeries(c, chart, i, plot, map);
        if (o.BarMode == ChartBarMode.Stacked && o.ShowStackTotals) DrawStackTotals(c, chart, plot, map);
        return PngWriter.WriteRgba(c.Width, c.Height, c.Pixels);
    }

    private static void DrawAnnotationBands(RgbaCanvas c, Chart chart, ChartRect plot, ChartMapper map) {
        foreach (var annotation in chart.Annotations) {
            if (!annotation.EndValue.HasValue) continue;
            var color = ApplyOpacity(annotation.Color, annotation.Opacity);
            if (annotation.Kind == ChartAnnotationKind.HorizontalBand) {
                var y1 = Clamp(map.Y(annotation.Value), plot.Top, plot.Bottom);
                var y2 = Clamp(map.Y(annotation.EndValue.Value), plot.Top, plot.Bottom);
                c.FillRect(plot.Left, Math.Min(y1, y2), plot.Width, Math.Abs(y2 - y1), color);
                DrawTinyAnnotationLabel(c, chart, annotation, plot.Left + 8, Math.Min(y1, y2) + 8);
            } else if (annotation.Kind == ChartAnnotationKind.VerticalBand) {
                var x1 = Clamp(map.X(annotation.Value), plot.Left, plot.Right);
                var x2 = Clamp(map.X(annotation.EndValue.Value), plot.Left, plot.Right);
                c.FillRect(Math.Min(x1, x2), plot.Top, Math.Abs(x2 - x1), plot.Height, color);
                DrawTinyAnnotationLabel(c, chart, annotation, Math.Min(x1, x2) + 8, plot.Top + 8);
            }
        }
    }

    private static void DrawAnnotationLines(RgbaCanvas c, Chart chart, ChartRect plot, ChartMapper map) {
        foreach (var annotation in chart.Annotations) {
            if (annotation.Kind == ChartAnnotationKind.HorizontalLine) {
                var y = Clamp(map.Y(annotation.Value), plot.Top, plot.Bottom);
                c.DrawLine(plot.Left, y, plot.Right, y, annotation.Color, 1);
                DrawTinyAnnotationLabel(c, chart, annotation, plot.Right - EstimateTinyTextWidth(annotation.Label) - 4, y - 10);
            } else if (annotation.Kind == ChartAnnotationKind.VerticalLine) {
                var x = Clamp(map.X(annotation.Value), plot.Left, plot.Right);
                c.DrawLine(x, plot.Top, x, plot.Bottom, annotation.Color, 1);
                DrawTinyAnnotationLabel(c, chart, annotation, x + 5, plot.Top + 8);
            }
        }
    }

    private static void DrawTinyAnnotationLabel(RgbaCanvas c, Chart chart, ChartAnnotation annotation, double x, double y) {
        if (string.IsNullOrWhiteSpace(annotation.Label)) return;
        c.DrawTextTiny(x, y, annotation.Label, chart.Options.Theme.Text, 1);
    }

    private static void DrawHorizontalBarGrid(RgbaCanvas c, Chart chart, ChartRect plot, ChartMapper map, IReadOnlyList<double> xTicks, IReadOnlyList<double> categories) {
        var o = chart.Options;
        var t = o.Theme;
        foreach (var xv in xTicks) {
            var x = map.X(xv);
            if (o.ShowGrid) c.DrawLine(x, plot.Top, x, plot.Bottom, ChartColor.FromRgba(t.Grid.R, t.Grid.G, t.Grid.B, (byte)(t.Grid.A / 2)), 1);
            if (o.ShowAxes) c.DrawTextTiny(x - EstimateTinyTextWidth(FormatValue(chart, xv)) / 2.0, plot.Bottom + 10, FormatValue(chart, xv), t.MutedText, 1);
        }

        foreach (var category in categories) {
            var y = map.Y(category);
            if (o.ShowGrid) c.DrawLine(plot.Left, y, plot.Right, y, ChartColor.FromRgba(t.Grid.R, t.Grid.G, t.Grid.B, (byte)(t.Grid.A / 3)), 1);
            if (o.ShowAxes) c.DrawTextTiny(plot.Left - EstimateTinyTextWidth(FormatX(chart, category)) - 8, y - 5, FormatX(chart, category), t.MutedText, 1);
        }

        if (o.ShowAxes) {
            var zeroX = map.X(0);
            if (zeroX > plot.Left && zeroX < plot.Right) c.DrawLine(zeroX, plot.Top, zeroX, plot.Bottom, t.Axis, 1);
            c.DrawLine(plot.Left, plot.Bottom, plot.Right, plot.Bottom, t.Axis, 1);
            c.DrawLine(plot.Left, plot.Top, plot.Left, plot.Bottom, t.Axis, 1);
        }
    }

    private static void DrawSeries(RgbaCanvas c, Chart chart, int index, ChartRect plot, ChartMapper map) {
        var s = chart.Series[index]; var color = s.Color ?? chart.Options.Theme.Palette[index % chart.Options.Theme.Palette.Length];
        if (s.Kind == ChartSeriesKind.HorizontalBar) {
            var layout = HorizontalBarLayout(chart, plot, index);
            var zeroX = Math.Min(plot.Right, Math.Max(plot.Left, map.X(0)));
            foreach (var p in s.Points) {
                var valueX = map.X(p.Y);
                var left = Math.Min(zeroX, valueX);
                var width = Math.Abs(valueX - zeroX);
                var y = map.Y(p.X) + layout.Offset - layout.BarHeight / 2;
                c.FillRect(left, y, width, layout.BarHeight, color);
                if (chart.Options.ShowDataLabels) {
                    var label = FormatValue(chart, p.Y);
                    var labelX = p.Y >= 0 ? Math.Min(plot.Right - EstimateTinyTextWidth(label), left + width + 6) : Math.Max(plot.Left + 2, left - EstimateTinyTextWidth(label) - 6);
                    c.DrawTextTiny(labelX, y + layout.BarHeight / 2 - 5, label, chart.Options.Theme.Text, 1);
                }
            }

            return;
        }

        if (s.Kind == ChartSeriesKind.Bar) {
            var layout = BarLayout(chart, plot, index);
            var zeroY = Math.Min(plot.Bottom, Math.Max(plot.Top, map.Y(0)));
            foreach (var p in s.Points) {
                var baseValue = chart.Options.BarMode == ChartBarMode.Stacked ? StackBaseValue(chart, index, p) : 0;
                var y = map.Y(baseValue + p.Y);
                var baseY = chart.Options.BarMode == ChartBarMode.Stacked ? map.Y(baseValue) : zeroY;
                c.FillRect(map.X(p.X) + layout.Offset - layout.BarWidth / 2, Math.Min(y, baseY), layout.BarWidth, Math.Abs(baseY - y), color);
                if (chart.Options.ShowDataLabels) {
                    var label = FormatValue(chart, p.Y);
                    var segmentHeight = Math.Abs(baseY - y);
                    if (chart.Options.BarMode == ChartBarMode.Stacked && segmentHeight < 14) continue;
                    var labelY = chart.Options.BarMode == ChartBarMode.Stacked ? Math.Min(y, baseY) + segmentHeight / 2 - 4 : Math.Min(y, baseY) - 12;
                    c.DrawTextTiny(map.X(p.X) + layout.Offset - EstimateTinyTextWidth(label) / 2.0, labelY, label, chart.Options.Theme.Text, 1);
                }
            }
            return;
        }
        if (s.Kind == ChartSeriesKind.Area && s.Points.Count > 0) {
            var zeroY = Math.Min(plot.Bottom, Math.Max(plot.Top, map.Y(0)));
            var polygon = new List<ChartPoint>(s.Points.Count + 2) {
                new(map.X(s.Points[0].X), zeroY)
            };
            foreach (var p in s.Points) polygon.Add(new ChartPoint(map.X(p.X), map.Y(p.Y)));
            polygon.Add(new ChartPoint(map.X(s.Points[s.Points.Count - 1].X), zeroY));
            c.FillPolygon(polygon, ChartColor.FromRgba(color.R, color.G, color.B, 58));
        }
        for (var i = 1; i < s.Points.Count; i++) {
            var a = s.Points[i - 1]; var b = s.Points[i];
            c.DrawLine(map.X(a.X), map.Y(a.Y), map.X(b.X), map.Y(b.Y), color, Math.Max(1, (int)Math.Round(s.StrokeWidth)));
        }
        if (s.Kind == ChartSeriesKind.Scatter || s.Kind == ChartSeriesKind.Line) foreach (var p in s.Points) c.DrawCircle(map.X(p.X), map.Y(p.Y), 4, color);
        if (chart.Options.ShowDataLabels) {
            foreach (var p in s.Points) {
                var label = FormatValue(chart, p.Y);
                c.DrawTextTiny(map.X(p.X) - EstimateTinyTextWidth(label) / 2.0, map.Y(p.Y) - 12, label, chart.Options.Theme.Text, 1);
            }
        }
    }

    private static void DrawPieLike(RgbaCanvas c, Chart chart, ChartRect plot) {
        var series = chart.Series[0];
        var values = new List<ChartPoint>();
        foreach (var point in series.Points) if (point.Y > 0) values.Add(point);
        if (values.Count == 0) return;

        var total = 0d;
        foreach (var value in values) total += value.Y;
        var radius = Math.Max(1, Math.Min(plot.Width, plot.Height) * 0.38);
        var cx = plot.Left + plot.Width * 0.42;
        var cy = plot.Top + plot.Height / 2;
        var inner = series.Kind == ChartSeriesKind.Donut ? radius * 0.58 : 0;
        var start = -Math.PI / 2;

        for (var i = 0; i < values.Count; i++) {
            var sweep = values[i].Y / total * Math.PI * 2;
            var end = start + sweep;
            var color = chart.Options.Theme.Palette[i % chart.Options.Theme.Palette.Length];
            c.FillRingSlice(cx, cy, radius, inner, start, end, color);
            start = end;
        }

        if (series.Kind == ChartSeriesKind.Donut) {
            var totalLabel = FormatValue(chart, total);
            c.DrawTextTiny(cx - EstimateTinyTextWidth(totalLabel), cy - 8, totalLabel, chart.Options.Theme.Text, 2);
        }

        if (chart.Options.ShowLegend) DrawSliceLegend(c, chart, values, plot, total);
    }

    private static void DrawSliceLegend(RgbaCanvas c, Chart chart, IReadOnlyList<ChartPoint> values, ChartRect plot, double total) {
        var x = plot.Left + plot.Width * 0.72;
        var y = plot.Top + Math.Max(24, plot.Height * 0.18);
        for (var i = 0; i < values.Count; i++) {
            var color = chart.Options.Theme.Palette[i % chart.Options.Theme.Palette.Length];
            c.FillRect(x, y - 8, 9, 9, color);
            c.DrawTextTiny(x + 14, y - 8, SliceLabel(chart, values[i], i), chart.Options.Theme.Text, 1);
            c.DrawTextTiny(plot.Right - 34, y - 8, FormatPercent(values[i].Y / total), chart.Options.Theme.MutedText, 1);
            y += 18;
        }
    }

    private static string FormatNumber(double v) => Math.Abs(v) >= 1000 ? (v / 1000).ToString("0.#") + "K" : v.ToString("0.#");
    private static string FormatValue(Chart chart, double value) {
        var formatter = chart.Options.ValueFormatter;
        if (formatter == null) return FormatNumber(value);
        return formatter(value) ?? string.Empty;
    }
    private static string FormatPercent(double v) => v.ToString("0.#%");
    private static double Clamp(double value, double min, double max) => Math.Max(min, Math.Min(max, value));

    private static ChartColor ApplyOpacity(ChartColor color, double opacity) {
        var alpha = (byte)Math.Max(0, Math.Min(255, Math.Round(color.A * Math.Max(0, Math.Min(1, opacity)))));
        return ChartColor.FromRgba(color.R, color.G, color.B, alpha);
    }

    private static IReadOnlyList<double> GetXTicks(Chart chart, ChartRange range) {
        if (chart.Options.XAxisLabels.Count == 0) return ChartTicks.GenerateInside(range.MinX, range.MaxX, chart.Options.TickCount);
        var ticks = new List<double>();
        foreach (var label in chart.Options.XAxisLabels) {
            if (label.Value >= range.MinX && label.Value <= range.MaxX) ticks.Add(label.Value);
        }

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

        if (categories.Count > 0) {
            var values = new List<double>();
            foreach (var category in categories) values.Add(category);
            return values;
        }

        return ChartTicks.GenerateInside(range.MinY, range.MaxY, chart.Options.TickCount);
    }

    private static string FormatX(Chart chart, double value) {
        foreach (var label in chart.Options.XAxisLabels) {
            if (Math.Abs(label.Value - value) < 0.000001) return label.Text;
        }

        return FormatNumber(value);
    }

    private static int EstimateTinyTextWidth(string value) => value.Length * 4;
    private static bool IsPieLike(Chart chart) => chart.Series.Count > 0 && (chart.Series[0].Kind == ChartSeriesKind.Pie || chart.Series[0].Kind == ChartSeriesKind.Donut);
    private static bool IsHorizontalBarChart(Chart chart) {
        foreach (var series in chart.Series) if (series.Kind == ChartSeriesKind.HorizontalBar) return true;
        return false;
    }

    private static ChartRect ApplyHorizontalBarReserve(Chart chart, ChartRect plot, IReadOnlyList<double> categories) {
        if (!chart.Options.ShowAxes || categories.Count == 0) return plot;
        var widest = 0;
        foreach (var category in categories) widest = Math.Max(widest, EstimateTinyTextWidth(FormatX(chart, category)));
        var desiredLeft = Math.Max(plot.Left, widest + 42);
        var maxLeft = Math.Max(plot.Left, chart.Options.Size.Width - chart.Options.Padding.Right - 120);
        var adjustedLeft = Math.Min(desiredLeft, maxLeft);
        var leftShift = Math.Max(0, adjustedLeft - plot.Left);
        var rightReserve = HorizontalValueLabelReserve(chart);
        if (leftShift <= 0 && rightReserve <= 0) return plot;
        return new ChartRect(plot.X + leftShift, plot.Y, Math.Max(1, plot.Width - leftShift - rightReserve), plot.Height);
    }

    private static double HorizontalValueLabelReserve(Chart chart) {
        if (!chart.Options.ShowDataLabels) return 0;
        var widest = 0;
        foreach (var series in chart.Series) {
            if (series.Kind != ChartSeriesKind.HorizontalBar) continue;
            foreach (var point in series.Points) widest = Math.Max(widest, EstimateTinyTextWidth(FormatValue(chart, point.Y)));
        }

        return widest == 0 ? 0 : Math.Min(72, widest + 16);
    }

    private static void ApplyHorizontalValueBounds(Chart chart, ChartRange range, IReadOnlyList<double> xTicks) {
        var min = xTicks[0];
        var max = xTicks[xTicks.Count - 1];
        if (chart.Options.ShowDataLabels) {
            var span = Math.Max(1, max - min);
            var hasPositive = false;
            var hasNegative = false;
            foreach (var series in chart.Series) {
                if (series.Kind != ChartSeriesKind.HorizontalBar) continue;
                foreach (var point in series.Points) {
                    if (point.Y > 0) hasPositive = true;
                    if (point.Y < 0) hasNegative = true;
                }
            }

            if (hasPositive) max += span * 0.08;
            if (hasNegative) min -= span * 0.08;
        }

        range.SetXBounds(min, max);
    }

    private static BarLayoutInfo BarLayout(Chart chart, ChartRect plot, int seriesIndex) {
        var barSeries = new List<int>();
        for (var i = 0; i < chart.Series.Count; i++) {
            if (chart.Series[i].Kind == ChartSeriesKind.Bar) barSeries.Add(i);
        }

        var groupCount = chart.Options.BarMode == ChartBarMode.Stacked ? 1 : Math.Max(1, barSeries.Count);
        var groupPosition = chart.Options.BarMode == ChartBarMode.Stacked ? 0 : Math.Max(0, barSeries.IndexOf(seriesIndex));
        var xValues = new HashSet<double>();
        foreach (var index in barSeries) {
            foreach (var point in chart.Series[index].Points) xValues.Add(point.X);
        }

        var categoryCount = Math.Max(1, xValues.Count);
        var slotWidth = plot.Width / categoryCount;
        var groupWidth = slotWidth * (groupCount == 1 ? 0.58 : 0.74);
        var gap = groupCount == 1 ? 0 : Math.Min(4, groupWidth * 0.08);
        var barWidth = Math.Max(3, (groupWidth - gap * (groupCount - 1)) / groupCount);
        var offset = (groupPosition - (groupCount - 1) / 2.0) * (barWidth + gap);
        return new BarLayoutInfo(barWidth, offset);
    }

    private static HorizontalBarLayoutInfo HorizontalBarLayout(Chart chart, ChartRect plot, int seriesIndex) {
        var horizontalSeries = new List<int>();
        for (var i = 0; i < chart.Series.Count; i++) {
            if (chart.Series[i].Kind == ChartSeriesKind.HorizontalBar) horizontalSeries.Add(i);
        }

        var groupCount = Math.Max(1, horizontalSeries.Count);
        var groupPosition = Math.Max(0, horizontalSeries.IndexOf(seriesIndex));
        var yValues = new HashSet<double>();
        foreach (var index in horizontalSeries) {
            foreach (var point in chart.Series[index].Points) yValues.Add(point.X);
        }

        var categoryCount = Math.Max(1, yValues.Count);
        var slotHeight = plot.Height / categoryCount;
        var groupHeight = slotHeight * (groupCount == 1 ? 0.56 : 0.76);
        var gap = groupCount == 1 ? 0 : Math.Min(4, groupHeight * 0.08);
        var barHeight = Math.Max(3, Math.Min(30, (groupHeight - gap * (groupCount - 1)) / groupCount));
        var offset = (groupPosition - (groupCount - 1) / 2.0) * (barHeight + gap);
        return new HorizontalBarLayoutInfo(barHeight, offset);
    }

    private static double StackBaseValue(Chart chart, int seriesIndex, ChartPoint point) {
        var sum = 0.0;
        for (var i = 0; i < seriesIndex; i++) {
            var series = chart.Series[i];
            if (series.Kind != ChartSeriesKind.Bar) continue;
            foreach (var candidate in series.Points) {
                if (Math.Abs(candidate.X - point.X) >= 0.000001) continue;
                if ((point.Y >= 0 && candidate.Y >= 0) || (point.Y < 0 && candidate.Y < 0)) sum += candidate.Y;
                break;
            }
        }

        return sum;
    }

    private static void DrawStackTotals(RgbaCanvas c, Chart chart, ChartRect plot, ChartMapper map) {
        var positiveTotals = new Dictionary<double, double>();
        var negativeTotals = new Dictionary<double, double>();
        foreach (var series in chart.Series) {
            if (series.Kind != ChartSeriesKind.Bar) continue;
            foreach (var point in series.Points) AddStackTotal(point.Y >= 0 ? positiveTotals : negativeTotals, point.X, point.Y);
        }

        DrawStackTotalSet(c, chart, plot, map, positiveTotals, -12);
        DrawStackTotalSet(c, chart, plot, map, negativeTotals, 8);
    }

    private static void DrawStackTotalSet(RgbaCanvas c, Chart chart, ChartRect plot, ChartMapper map, Dictionary<double, double> totals, double offset) {
        foreach (var item in totals) {
            if (Math.Abs(item.Value) < 0.000001) continue;
            var label = FormatValue(chart, item.Value);
            var x = Clamp(map.X(item.Key) - EstimateTinyTextWidth(label) / 2.0, plot.Left + 2, plot.Right - EstimateTinyTextWidth(label) - 2);
            var y = Clamp(map.Y(item.Value) + offset, plot.Top + 2, plot.Bottom - 8);
            c.DrawTextTiny(x, y, label, chart.Options.Theme.Text, 1);
        }
    }

    private static void AddStackTotal(Dictionary<double, double> totals, double x, double y) {
        double current;
        totals.TryGetValue(x, out current);
        totals[x] = current + y;
    }

    private static string SliceLabel(Chart chart, ChartPoint point, int index) {
        foreach (var label in chart.Options.XAxisLabels) {
            if (Math.Abs(label.Value - point.X) < 0.000001) return label.Text;
        }

        return "Slice " + (index + 1);
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

}
