using System;
using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Raster;

public sealed partial class PngChartRenderer {
    private static void DrawTimeline(RgbaCanvas c, Chart chart, ChartRect plot) {
        var items = BuildTimelineItems(chart);
        if (items.Count == 0) return;

        var min = double.PositiveInfinity;
        var max = double.NegativeInfinity;
        foreach (var item in items) {
            min = Math.Min(min, item.Start);
            max = Math.Max(max, item.End);
        }

        ApplyTimelineAxisBounds(chart, ref min, ref max);
        var tickFontSize = PngTickFontSize(chart);
        var dataFontSize = chart.Options.Theme.DataLabelFontSize;
        plot = ApplyPngTimelineReserve(chart, plot, items, tickFontSize);
        var rowHeight = Math.Max(20, Math.Min(34, plot.Height / items.Count * 0.56));
        var slotHeight = plot.Height / items.Count;
        var ticks = ChartTicks.Generate(min, max, Math.Min(6, Math.Max(3, chart.Options.TickCount)));
        var tickLabelWidth = Math.Max(18, plot.Width / Math.Max(1, ticks.Count - 1) - 6);
        var rowLabelWidth = Math.Max(8, plot.Left - 24);

        foreach (var tick in ticks) {
            var x = ProjectTimelineX(tick, min, max, plot);
            if (chart.Options.ShowGrid) c.DrawLine(x, plot.Top, x, plot.Bottom, ApplyOpacity(chart.Options.Theme.Grid, ChartVisualPrimitives.TimelineGridOpacity), ChartVisualPrimitives.GridStrokeWidth);
            if (chart.Options.ShowAxes) {
                var rawLabel = FormatTimelineTick(chart, tick);
                var labelFontSize = TextFontSizeForWidth(rawLabel, tickLabelWidth, tickFontSize);
                var label = TrimPngLabelToWidth(rawLabel, labelFontSize, tickLabelWidth);
                var width = EstimatePngTextWidth(label, labelFontSize);
                c.DrawText(Clamp(x - width / 2.0, plot.Left + 2, plot.Right - width - 2), plot.Bottom + 22 - labelFontSize + 1, label, chart.Options.Theme.MutedText, labelFontSize);
            }
        }

        for (var i = 0; i < items.Count; i++) {
            var item = items[i];
            var y = plot.Top + i * slotHeight + (slotHeight - rowHeight) / 2;
            var x1 = ProjectTimelineX(item.Start, min, max, plot);
            var x2 = ProjectTimelineX(item.End, min, max, plot);
            var left = Math.Min(x1, x2);
            var width = Math.Max(2, Math.Abs(x2 - x1));
            if (chart.Options.ShowGrid) c.DrawLine(plot.Left, y + rowHeight / 2, plot.Right, y + rowHeight / 2, ApplyOpacity(chart.Options.Theme.Grid, ChartVisualPrimitives.TimelineRowGridOpacity), ChartVisualPrimitives.GridStrokeWidth);
            if (chart.Options.ShowAxes) {
                var rowLabelFontSize = TextFontSizeForEmphasizedWidth(item.Name, rowLabelWidth, tickFontSize);
                var rowLabel = TrimReadablePngLabelToWidth(item.Name, rowLabelFontSize, rowLabelWidth);
                if (rowLabel.Length > 0) c.DrawTextEmphasized(plot.Left - EstimatePngEmphasizedTextWidth(rowLabel, rowLabelFontSize) - 14, y + rowHeight / 2 - rowLabelFontSize / 2, rowLabel, chart.Options.Theme.MutedText, rowLabelFontSize);
            }
            DrawTimelineRangeBar(c, chart, item.Color, left, y, width, rowHeight);
            if (item.ShowDataLabels && width >= Math.Max(72, EstimatePngEmphasizedTextWidth("100d", dataFontSize) + 14)) {
                var label = FormatTimelineDuration(chart, item.Start, item.End);
                DrawReadablePngLabelCentered(c, new ChartRect(left, y, width, rowHeight), label, ChartColorMath.TextOnBackground(item.Color), item.Color, dataFontSize);
            }
        }

        if (chart.Options.ShowAxes) {
            c.DrawLine(plot.Left, plot.Bottom, plot.Right, plot.Bottom, chart.Options.Theme.Axis, ChartVisualPrimitives.AxisStrokeWidth);
            DrawTimelineAxisTitles(c, chart, plot);
        }
    }

    private static ChartRect ApplyPngTimelineReserve(Chart chart, ChartRect plot, IReadOnlyList<TimelineItem> items, double tickFontSize) {
        var widest = 0.0;
        foreach (var item in items) widest = Math.Max(widest, EstimatePngEmphasizedTextWidth(item.Name, tickFontSize));
        var yAxisReserve = string.IsNullOrWhiteSpace(chart.YAxisTitle) ? 0 : 28;
        var desiredLeft = Math.Max(plot.Left, widest + yAxisReserve + 64);
        var maxLeft = Math.Max(plot.Left, chart.Options.Size.Width - chart.Options.Padding.Right - 180);
        var shift = Math.Max(0, Math.Min(desiredLeft, maxLeft) - plot.Left);
        var bottomReserve = 52 + (string.IsNullOrWhiteSpace(chart.XAxisTitle) ? 0 : 18);
        return new ChartRect(plot.X + shift, plot.Y, Math.Max(1, plot.Width - shift), Math.Max(1, plot.Height - bottomReserve));
    }

    private static void DrawTimelineRangeBar(RgbaCanvas c, Chart chart, ChartColor color, double left, double y, double width, double rowHeight) {
        var radius = Math.Min(ChartVisualPrimitives.TimelineItemCornerRadiusMax, rowHeight / 2);
        c.FillRoundedRectVerticalGradient(left, y, width, rowHeight, radius, TimelineItemGradientTop(color), TimelineItemGradientBottom(color));
        c.StrokeRoundedRect(left, y, width, rowHeight, radius, ApplyOpacity(chart.Options.Theme.CardBackground, ChartVisualPrimitives.TimelineItemBorderOpacity), ChartVisualPrimitives.TimelineItemBorderStrokeWidth);

        var inset = Math.Min(radius, width / 3);
        var x1 = left + inset;
        var x2 = left + width - inset;
        if (x2 > x1) c.DrawLine(x1, y + ChartVisualPrimitives.TimelineItemHighlightOffsetY, x2, y + ChartVisualPrimitives.TimelineItemHighlightOffsetY, ApplyOpacity(ChartColor.White, ChartVisualPrimitives.TimelineItemHighlightOpacity), ChartVisualPrimitives.GridStrokeWidth);
    }

    private static bool IsTimelineChart(Chart chart) => ChartSeriesKindTraits.ContainsKind(chart, ChartSeriesKind.Timeline);

    private static List<TimelineItem> BuildTimelineItems(Chart chart) {
        var items = new List<TimelineItem>();
        for (var i = 0; i < chart.Series.Count; i++) {
            var series = chart.Series[i];
            if (series.Kind != ChartSeriesKind.Timeline || series.Points.Count == 0) continue;
            var point = series.Points[0];
            var start = Math.Min(point.X, point.Y);
            var end = Math.Max(point.X, point.Y);
            items.Add(new TimelineItem(series.Name, start, end, series.Color ?? chart.Options.Theme.Palette[i % chart.Options.Theme.Palette.Length], ShouldDrawDataLabels(chart, series)));
        }

        return items;
    }

    private static double ProjectTimelineX(double value, double min, double max, ChartRect plot) {
        return plot.Left + (value - min) / Math.Max(0.000001, max - min) * plot.Width;
    }

    private static void ApplyTimelineAxisBounds(Chart chart, ref double min, ref double max) {
        var hasMinimum = chart.Options.XAxisMinimum.HasValue;
        var hasMaximum = chart.Options.XAxisMaximum.HasValue;
        if (hasMinimum) min = chart.Options.XAxisMinimum!.Value;
        if (hasMaximum) max = chart.Options.XAxisMaximum!.Value;
        if (max <= min || Math.Abs(max - min) < 0.000001) max = min + 1;
        var span = max - min;
        if (!hasMinimum) min -= span * 0.04;
        if (!hasMaximum) max += span * 0.04;
    }

    private static string FormatTimelineTick(Chart chart, double value) {
        foreach (var label in chart.Options.XAxisLabels) {
            if (Math.Abs(label.Value - value) < 0.000001) return label.Text;
        }

        if (chart.Options.XAxisValueFormatter != null) return chart.Options.XAxisValueFormatter(value) ?? string.Empty;

        try {
            return DateTime.FromOADate(value).ToString("MMM d", CultureInfo.InvariantCulture);
        } catch (ArgumentException) {
            return FormatNumber(value);
        }
    }

    private static string FormatTimelineDuration(Chart chart, double start, double end) {
        var duration = Math.Max(1, Math.Abs(end - start));
        if (chart.Options.ValueFormatter != null) return chart.Options.ValueFormatter(duration) ?? string.Empty;
        return ((int)Math.Round(duration)).ToString(CultureInfo.InvariantCulture) + "d";
    }

    private static ChartColor TimelineItemGradientTop(ChartColor color) => ChartMarkSurface.TimelineItemGradientTop(color);

    private static ChartColor TimelineItemGradientBottom(ChartColor color) => ChartMarkSurface.TimelineItemGradientBottom(color);

    private static ChartColor GanttTaskGradientTop(ChartColor color) => ChartMarkSurface.GanttTaskGradientTop(color);

    private static ChartColor GanttTaskGradientBottom(ChartColor color) => ChartMarkSurface.GanttTaskGradientBottom(color);

    private static void DrawTimelineAxisTitles(RgbaCanvas c, Chart chart, ChartRect plot) {
        if (!string.IsNullOrWhiteSpace(chart.XAxisTitle)) {
            DrawPngXAxisTitle(c, chart, plot, plot.Bottom + 49, PngAxisTitleFontSize(chart));
        }

        DrawYAxisTitle(c, chart, plot, PngAxisTitleFontSize(chart));
    }

    private readonly struct TimelineItem {
        public TimelineItem(string name, double start, double end, ChartColor color, bool showDataLabels) {
            Name = name;
            Start = start;
            End = end;
            Color = color;
            ShowDataLabels = showDataLabels;
        }

        public string Name { get; }

        public double Start { get; }

        public double End { get; }

        public ChartColor Color { get; }

        public bool ShowDataLabels { get; }
    }
}
