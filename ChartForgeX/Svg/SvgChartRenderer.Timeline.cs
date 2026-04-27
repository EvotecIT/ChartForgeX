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
    private static void DrawTimeline(StringBuilder sb, Chart chart, ChartRect basePlot) {
        var items = BuildTimelineItems(chart);
        if (items.Count == 0) return;

        var t = chart.Options.Theme;
        var min = items.Min(item => item.Start);
        var max = items.Max(item => item.End);
        if (Math.Abs(max - min) < 0.000001) max = min + 1;
        var span = max - min;
        min -= span * 0.04;
        max += span * 0.04;
        var plot = ApplyTimelineReserve(chart, basePlot, items);
        var rowHeight = Math.Max(20, Math.Min(34, plot.Height / items.Count * 0.56));
        var slotHeight = plot.Height / items.Count;
        var ticks = ChartTicks.Generate(min, max, Math.Min(6, Math.Max(3, chart.Options.TickCount)));
        var tickLabelWidth = Math.Max(18, plot.Width / Math.Max(1, ticks.Count - 1) - 6);
        var rowLabelWidth = Math.Max(8, plot.Left - chart.Options.Padding.Left - 2);

        sb.AppendLine("<g data-cfx-role=\"timeline\">");
        foreach (var tick in ticks) {
            var x = ProjectTimelineX(tick, min, max, plot);
            if (chart.Options.ShowGrid) sb.AppendLine($"<line x1=\"{F(x)}\" y1=\"{F(plot.Top)}\" x2=\"{F(x)}\" y2=\"{F(plot.Bottom)}\" stroke=\"{t.Grid.ToCss()}\" stroke-width=\"1\" opacity=\"0.55\"/>");
            if (chart.Options.ShowAxes) {
                var label = FormatTimelineTick(chart, tick);
                label = TrimSvgLabelToWidth(label, t.TickLabelFontSize, tickLabelWidth);
                var anchor = EdgeAwareAnchor(label, x, plot, t.TickLabelFontSize);
                var labelX = EdgeAwareTextX(label, x, plot, t.TickLabelFontSize);
                sb.AppendLine($"<text data-cfx-role=\"timeline-tick-label\" x=\"{F(labelX)}\" y=\"{F(plot.Bottom + 22)}\" text-anchor=\"{anchor}\" fill=\"{t.MutedText.ToCss()}\" font-family=\"{SvgFontFamily(t.FontFamily)}\" font-size=\"{F(t.TickLabelFontSize)}\">{Escape(label)}</text>");
            }
        }

        for (var i = 0; i < items.Count; i++) {
            var item = items[i];
            var y = plot.Top + i * slotHeight + (slotHeight - rowHeight) / 2;
            var x1 = ProjectTimelineX(item.Start, min, max, plot);
            var x2 = ProjectTimelineX(item.End, min, max, plot);
            var left = Math.Min(x1, x2);
            var width = Math.Max(2, Math.Abs(x2 - x1));
            var duration = FormatTimelineDuration(item.Start, item.End);
            var summary = BuildTimelineSummary(chart, item, duration);
            if (chart.Options.ShowGrid) sb.AppendLine($"<line x1=\"{F(plot.Left)}\" y1=\"{F(y + rowHeight / 2)}\" x2=\"{F(plot.Right)}\" y2=\"{F(y + rowHeight / 2)}\" stroke=\"{t.Grid.ToCss()}\" stroke-width=\"1\" opacity=\"0.22\"/>");
            if (chart.Options.ShowAxes) {
                var rowLabel = TrimSvgLabelToWidth(item.Name, t.TickLabelFontSize, rowLabelWidth);
                sb.AppendLine($"<text data-cfx-role=\"timeline-row-label\" x=\"{F(plot.Left - 14)}\" y=\"{F(y + rowHeight / 2)}\" text-anchor=\"end\" dominant-baseline=\"middle\" fill=\"{t.MutedText.ToCss()}\" font-family=\"{SvgFontFamily(t.FontFamily)}\" font-size=\"{F(t.TickLabelFontSize)}\" font-weight=\"650\">{Escape(rowLabel)}</text>");
            }
            sb.AppendLine($"<rect data-cfx-role=\"timeline-item\" data-cfx-row=\"{i}\" data-cfx-start=\"{F(item.Start)}\" data-cfx-end=\"{F(item.End)}\" data-cfx-duration=\"{Escape(duration)}\" role=\"img\" aria-label=\"{Escape(summary)}\" x=\"{F(left)}\" y=\"{F(y)}\" width=\"{F(width)}\" height=\"{F(rowHeight)}\" rx=\"{F(Math.Min(8, rowHeight / 2))}\" fill=\"{item.Color.ToCss()}\" opacity=\"0.94\"/>");
            if (chart.Options.ShowDataLabels && width >= 72) {
                DrawSvgTextCenteredX(sb, chart, "data-label", duration, left + width / 2, y + rowHeight / 2, HeatmapTextColor(item.Color), t.DataLabelFontSize, width - 6, "750");
            }
        }

        if (chart.Options.ShowAxes) {
            sb.AppendLine($"<line x1=\"{F(plot.Left)}\" y1=\"{F(plot.Bottom)}\" x2=\"{F(plot.Right)}\" y2=\"{F(plot.Bottom)}\" stroke=\"{t.Axis.ToCss()}\" stroke-width=\"1.2\"/>");
            DrawSvgXAxisTitle(sb, chart, plot, plot.Bottom + 49, "timeline-x-axis-title");
            if (!string.IsNullOrWhiteSpace(chart.YAxisTitle)) {
                var widestLabel = items.Max(item => EstimateTextWidth(item.Name, t.TickLabelFontSize));
                var axisX = Math.Max(24, plot.Left - widestLabel - 46);
                DrawSvgYAxisTitle(sb, chart, plot, axisX, "timeline-y-axis-title");
            }
        }

        sb.AppendLine("</g>");
    }

    private static List<TimelineItem> BuildTimelineItems(Chart chart) {
        var items = new List<TimelineItem>();
        for (var i = 0; i < chart.Series.Count; i++) {
            var series = chart.Series[i];
            if (series.Kind != ChartSeriesKind.Timeline || series.Points.Count == 0) continue;
            var point = series.Points[0];
            var start = Math.Min(point.X, point.Y);
            var end = Math.Max(point.X, point.Y);
            items.Add(new TimelineItem(series.Name, start, end, series.Color ?? Color(chart, i)));
        }

        return items;
    }

    private static ChartRect ApplyTimelineReserve(Chart chart, ChartRect plot, IReadOnlyList<TimelineItem> items) {
        var t = chart.Options.Theme;
        var widest = items.Max(item => EstimateTextWidth(item.Name, t.TickLabelFontSize));
        var yAxisReserve = string.IsNullOrWhiteSpace(chart.YAxisTitle) ? 0 : 28;
        var desiredLeft = Math.Max(plot.Left, widest + yAxisReserve + 64);
        var maxLeft = Math.Max(plot.Left, chart.Options.Size.Width - chart.Options.Padding.Right - 180);
        var shift = Math.Max(0, Math.Min(desiredLeft, maxLeft) - plot.Left);
        var bottomReserve = 52 + (string.IsNullOrWhiteSpace(chart.XAxisTitle) ? 0 : 18);
        return new ChartRect(plot.X + shift, plot.Y, Math.Max(1, plot.Width - shift), Math.Max(1, plot.Height - bottomReserve));
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

    private static string BuildTimelineSummary(Chart chart, TimelineItem item, string duration) =>
        item.Name + ": " + FormatTimelineTick(chart, item.Start) + " to " + FormatTimelineTick(chart, item.End) + ", duration " + duration;

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
