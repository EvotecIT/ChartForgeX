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

        if (Math.Abs(max - min) < 0.000001) max = min + 1;
        var span = max - min;
        min -= span * 0.04;
        max += span * 0.04;
        var tickFontSize = PngTickFontSize(chart);
        var dataFontSize = chart.Options.Theme.DataLabelFontSize;
        plot = ApplyPngTimelineReserve(chart, plot, items, tickFontSize);
        var rowHeight = Math.Max(20, Math.Min(34, plot.Height / items.Count * 0.56));
        var slotHeight = plot.Height / items.Count;
        var ticks = ChartTicks.Generate(min, max, Math.Min(6, Math.Max(3, chart.Options.TickCount)));
        var tickLabelWidth = Math.Max(18, plot.Width / Math.Max(1, ticks.Count - 1) - 6);
        var rowLabelWidth = Math.Max(8, plot.Left - chart.Options.Padding.Left - 2);

        foreach (var tick in ticks) {
            var x = ProjectTimelineX(tick, min, max, plot);
            if (chart.Options.ShowGrid) c.DrawLine(x, plot.Top, x, plot.Bottom, ChartColor.FromRgba(chart.Options.Theme.Grid.R, chart.Options.Theme.Grid.G, chart.Options.Theme.Grid.B, (byte)(chart.Options.Theme.Grid.A / 2)), 1);
            if (chart.Options.ShowAxes) {
                var label = FormatTimelineTick(chart, tick);
                label = TrimReadablePngLabelToWidth(label, tickFontSize, tickLabelWidth);
                var width = EstimatePngTextWidth(label, tickFontSize);
                c.DrawText(Clamp(x - width / 2.0, plot.Left + 2, plot.Right - width - 2), plot.Bottom + 22 - tickFontSize + 1, label, chart.Options.Theme.MutedText, tickFontSize);
            }
        }

        for (var i = 0; i < items.Count; i++) {
            var item = items[i];
            var y = plot.Top + i * slotHeight + (slotHeight - rowHeight) / 2;
            var x1 = ProjectTimelineX(item.Start, min, max, plot);
            var x2 = ProjectTimelineX(item.End, min, max, plot);
            var left = Math.Min(x1, x2);
            var width = Math.Max(2, Math.Abs(x2 - x1));
            if (chart.Options.ShowGrid) c.DrawLine(plot.Left, y + rowHeight / 2, plot.Right, y + rowHeight / 2, ChartColor.FromRgba(chart.Options.Theme.Grid.R, chart.Options.Theme.Grid.G, chart.Options.Theme.Grid.B, (byte)(chart.Options.Theme.Grid.A / 3)), 1);
            if (chart.Options.ShowAxes) {
                var rowLabel = TrimReadablePngLabelToWidth(item.Name, tickFontSize, rowLabelWidth);
                if (rowLabel.Length > 0) c.DrawTextEmphasized(plot.Left - EstimatePngEmphasizedTextWidth(rowLabel, tickFontSize) - 14, y + rowHeight / 2 - tickFontSize / 2, rowLabel, chart.Options.Theme.MutedText, tickFontSize);
            }
            c.FillRoundedRect(left, y, width, rowHeight, Math.Min(8, rowHeight / 2), item.Color);
            if (chart.Options.ShowDataLabels && width >= Math.Max(72, EstimatePngEmphasizedTextWidth("100d", dataFontSize) + 14)) {
                var label = FormatTimelineDuration(item.Start, item.End);
                DrawReadablePngLabelCentered(c, new ChartRect(left, y, width, rowHeight), label, HeatmapTextColor(item.Color), item.Color, dataFontSize);
            }
        }

        if (chart.Options.ShowAxes) {
            c.DrawLine(plot.Left, plot.Bottom, plot.Right, plot.Bottom, chart.Options.Theme.Axis, 1);
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

    private static bool IsTimelineChart(Chart chart) {
        foreach (var series in chart.Series) if (series.Kind == ChartSeriesKind.Timeline) return true;
        return false;
    }

    private static List<TimelineItem> BuildTimelineItems(Chart chart) {
        var items = new List<TimelineItem>();
        for (var i = 0; i < chart.Series.Count; i++) {
            var series = chart.Series[i];
            if (series.Kind != ChartSeriesKind.Timeline || series.Points.Count == 0) continue;
            var point = series.Points[0];
            var start = Math.Min(point.X, point.Y);
            var end = Math.Max(point.X, point.Y);
            items.Add(new TimelineItem(series.Name, start, end, series.Color ?? chart.Options.Theme.Palette[i % chart.Options.Theme.Palette.Length]));
        }

        return items;
    }

    private static double ProjectTimelineX(double value, double min, double max, ChartRect plot) {
        return plot.Left + (value - min) / Math.Max(0.000001, max - min) * plot.Width;
    }

    private static string FormatTimelineTick(Chart chart, double value) {
        foreach (var label in chart.Options.XAxisLabels) {
            if (Math.Abs(label.Value - value) < 0.000001) return label.Text;
        }

        try {
            return DateTime.FromOADate(value).ToString("MMM d", CultureInfo.InvariantCulture);
        } catch (ArgumentException) {
            return FormatNumber(value);
        }
    }

    private static string FormatTimelineDuration(double start, double end) {
        var days = Math.Max(1, (int)Math.Round(Math.Abs(end - start)));
        return days.ToString(CultureInfo.InvariantCulture) + "d";
    }

    private static void DrawTimelineAxisTitles(RgbaCanvas c, Chart chart, ChartRect plot) {
        if (!string.IsNullOrWhiteSpace(chart.XAxisTitle)) {
            DrawPngXAxisTitle(c, chart, plot, plot.Bottom + 49, PngAxisTitleFontSize(chart));
        }

        DrawYAxisTitle(c, chart, plot, PngAxisTitleFontSize(chart));
    }

    private readonly struct TimelineItem {
        public TimelineItem(string name, double start, double end, ChartColor color) {
            Name = name;
            Start = start;
            End = end;
            Color = color;
        }

        public string Name { get; }

        public double Start { get; }

        public double End { get; }

        public ChartColor Color { get; }
    }
}
