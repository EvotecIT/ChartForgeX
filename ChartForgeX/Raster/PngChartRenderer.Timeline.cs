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
        var labelWidth = 0;
        foreach (var item in items) {
            min = Math.Min(min, item.Start);
            max = Math.Max(max, item.End);
            labelWidth = Math.Max(labelWidth, EstimateTinyTextWidth(item.Name));
        }

        if (Math.Abs(max - min) < 0.000001) max = min + 1;
        var span = max - min;
        min -= span * 0.04;
        max += span * 0.04;
        plot = new ChartRect(plot.X + labelWidth + 12, plot.Y, Math.Max(1, plot.Width - labelWidth - 12), Math.Max(1, plot.Height - 42));
        var rowHeight = Math.Max(10, Math.Min(24, plot.Height / items.Count * 0.52));
        var slotHeight = plot.Height / items.Count;
        var ticks = ChartTicks.Generate(min, max, Math.Min(6, Math.Max(3, chart.Options.TickCount)));

        foreach (var tick in ticks) {
            var x = ProjectTimelineX(tick, min, max, plot);
            if (chart.Options.ShowGrid) c.DrawLine(x, plot.Top, x, plot.Bottom, ChartColor.FromRgba(chart.Options.Theme.Grid.R, chart.Options.Theme.Grid.G, chart.Options.Theme.Grid.B, (byte)(chart.Options.Theme.Grid.A / 2)), 1);
            if (chart.Options.ShowAxes) {
                var label = FormatTimelineTick(chart, tick);
                c.DrawTextTiny(x - EstimateTinyTextWidth(label) / 2.0, plot.Bottom + 10, label, chart.Options.Theme.MutedText, 1);
            }
        }

        for (var i = 0; i < items.Count; i++) {
            var item = items[i];
            var y = plot.Top + i * slotHeight + (slotHeight - rowHeight) / 2;
            var x1 = ProjectTimelineX(item.Start, min, max, plot);
            var x2 = ProjectTimelineX(item.End, min, max, plot);
            var left = Math.Min(x1, x2);
            var width = Math.Max(2, Math.Abs(x2 - x1));
            if (chart.Options.ShowAxes) c.DrawTextTiny(plot.Left - EstimateTinyTextWidth(item.Name) - 8, y + rowHeight / 2 - 4, item.Name, chart.Options.Theme.MutedText, 1);
            c.FillRect(left, y, width, rowHeight, item.Color);
        }

        if (chart.Options.ShowAxes) c.DrawLine(plot.Left, plot.Bottom, plot.Right, plot.Bottom, chart.Options.Theme.Axis, 1);
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
