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
    private static void DrawTimeline(StringBuilder sb, Chart chart, ChartRect basePlot, string id) {
        var items = BuildTimelineItems(chart);
        if (items.Count == 0) return;

        var t = chart.Options.Theme;
        var min = items.Min(item => item.Start);
        var max = items.Max(item => item.End);
        ApplyTimelineAxisBounds(chart, ref min, ref max);
        var plot = ApplyTimelineReserve(chart, basePlot, items);
        var rowHeight = Math.Max(20, Math.Min(34, plot.Height / items.Count * 0.56));
        var slotHeight = plot.Height / items.Count;
        var ticks = ChartTicks.Generate(min, max, Math.Min(6, Math.Max(3, chart.Options.TickCount)));
        var tickLabelWidth = Math.Max(18, plot.Width / Math.Max(1, ticks.Count - 1) - 6);
        var rowLabelWidth = Math.Max(8, plot.Left - 24);

        var writer = new SvgMarkupWriter(4096);
        writer
            .StartElement("g")
            .Attribute("data-cfx-role", "timeline")
            .EndStartElement()
            .Line();
        DrawTimelineItemGradients(writer, id, items);
        foreach (var tick in ticks) {
            var x = ProjectTimelineX(tick, min, max, plot);
            if (chart.Options.ShowGrid) {
                writer
                    .StartElement("line")
                    .Attribute("x1", x)
                    .Attribute("y1", plot.Top)
                    .Attribute("x2", x)
                    .Attribute("y2", plot.Bottom)
                    .Attribute("stroke", t.Grid.ToCss())
                    .Attribute("stroke-width", ChartVisualPrimitives.GridStrokeWidth)
                    .Attribute("opacity", ChartVisualPrimitives.TimelineGridOpacity)
                    .EndEmptyElement()
                    .Line();
            }

            if (chart.Options.ShowAxes) {
                var rawLabel = FormatTimelineTick(chart, tick);
                var labelFontSize = TextFontSizeForSvgWidth(rawLabel, tickLabelWidth, t.TickLabelFontSize);
                var label = TrimSvgLabelToWidth(rawLabel, labelFontSize, tickLabelWidth);
                var anchor = EdgeAwareAnchor(label, x, plot, labelFontSize);
                var labelX = EdgeAwareTextX(label, x, plot, labelFontSize);
                writer
                    .StartElement("text")
                    .Attribute("data-cfx-role", "timeline-tick-label")
                    .Attribute("x", labelX)
                    .Attribute("y", plot.Bottom + 22)
                    .Attribute("text-anchor", anchor)
                    .Attribute("fill", t.MutedText.ToCss())
                    .Attribute("font-family", TimelineFontFamily(t.FontFamily))
                    .Attribute("font-size", labelFontSize)
                    .Text(label)
                    .EndElement()
                    .Line();
            }
        }

        for (var i = 0; i < items.Count; i++) {
            var item = items[i];
            var y = plot.Top + i * slotHeight + (slotHeight - rowHeight) / 2;
            var x1 = ProjectTimelineX(item.Start, min, max, plot);
            var x2 = ProjectTimelineX(item.End, min, max, plot);
            var left = Math.Min(x1, x2);
            var width = Math.Max(2, Math.Abs(x2 - x1));
            var duration = FormatTimelineDuration(chart, item.Start, item.End);
            var summary = BuildTimelineSummary(chart, item, duration);
            if (chart.Options.ShowGrid) {
                writer
                    .StartElement("line")
                    .Attribute("x1", plot.Left)
                    .Attribute("y1", y + rowHeight / 2)
                    .Attribute("x2", plot.Right)
                    .Attribute("y2", y + rowHeight / 2)
                    .Attribute("stroke", t.Grid.ToCss())
                    .Attribute("stroke-width", ChartVisualPrimitives.GridStrokeWidth)
                    .Attribute("opacity", ChartVisualPrimitives.TimelineRowGridOpacity)
                    .EndEmptyElement()
                    .Line();
            }

            if (chart.Options.ShowAxes) {
                var rowLabelFontSize = TextFontSizeForSvgWidth(item.Name, rowLabelWidth, t.TickLabelFontSize);
                var rowLabel = TrimSvgLabelToWidth(item.Name, rowLabelFontSize, rowLabelWidth);
                writer
                    .StartElement("text")
                    .Attribute("data-cfx-role", "timeline-row-label")
                    .Attribute("x", plot.Left - 14)
                    .Attribute("y", y + rowHeight / 2)
                    .Attribute("text-anchor", "end")
                    .Attribute("dominant-baseline", "middle")
                    .Attribute("fill", t.MutedText.ToCss())
                    .Attribute("font-family", TimelineFontFamily(t.FontFamily))
                    .Attribute("font-size", rowLabelFontSize)
                    .Attribute("font-weight", "650")
                    .Text(rowLabel)
                    .EndElement()
                    .Line();
            }
            DrawTimelineRangeBar(writer, chart, item, id, i, left, y, width, rowHeight, duration, summary);
            if (item.ShowDataLabels && width >= 72) {
                DrawSvgTextCenteredX(writer, chart, "data-label", duration, left + width / 2, y + rowHeight / 2, HeatmapTextColor(item.Color), t.DataLabelFontSize, width - 6, "750");
            }
        }

        if (chart.Options.ShowAxes) {
            writer
                .StartElement("line")
                .Attribute("x1", plot.Left)
                .Attribute("y1", plot.Bottom)
                .Attribute("x2", plot.Right)
                .Attribute("y2", plot.Bottom)
                .Attribute("stroke", t.Axis.ToCss())
                .Attribute("stroke-width", ChartVisualPrimitives.AxisStrokeWidth)
                .EndEmptyElement()
                .Line();
            DrawTimelineSvgXAxisTitle(writer, chart, plot, plot.Bottom + 49, "timeline-x-axis-title");
            if (!string.IsNullOrWhiteSpace(chart.YAxisTitle)) {
                var widestLabel = items.Max(item => EstimateTextWidth(item.Name, t.TickLabelFontSize));
                var axisX = Math.Max(24, plot.Left - widestLabel - 46);
                DrawTimelineSvgYAxisTitle(writer, chart, plot, axisX, "timeline-y-axis-title");
            }
        }

        writer.EndElement().Line();
        sb.Append(writer.Build());
    }

    private static void DrawTimelineItemGradients(SvgMarkupWriter writer, string id, IReadOnlyList<TimelineItem> items) {
        writer.StartElement("defs").EndStartElement().Line();
        foreach (var item in items) {
            writer
                .StartElement("linearGradient")
                .Attribute("id", id + "-timelineFill" + item.SeriesIndex.ToString(CultureInfo.InvariantCulture))
                .Attribute("x1", "0")
                .Attribute("x2", "0")
                .Attribute("y1", "0")
                .Attribute("y2", "1")
                .EndStartElement()
                .StartElement("stop")
                .Attribute("offset", "0%")
                .Attribute("stop-color", TimelineItemGradientTop(item.Color).ToHex())
                .EndEmptyElement()
                .StartElement("stop")
                .Attribute("offset", "100%")
                .Attribute("stop-color", TimelineItemGradientBottom(item.Color).ToHex())
                .EndEmptyElement()
                .EndElement()
                .Line();
        }
        writer.EndElement().Line();
    }

    private static List<TimelineItem> BuildTimelineItems(Chart chart) {
        var items = new List<TimelineItem>();
        for (var i = 0; i < chart.Series.Count; i++) {
            var series = chart.Series[i];
            if (series.Kind != ChartSeriesKind.Timeline || series.Points.Count == 0) continue;
            var point = series.Points[0];
            var start = Math.Min(point.X, point.Y);
            var end = Math.Max(point.X, point.Y);
            items.Add(new TimelineItem(i, series.Name, start, end, series.Color ?? Color(chart, i), ShouldDrawDataLabels(chart, series)));
        }

        return items;
    }

    private static void DrawTimelineRangeBar(SvgMarkupWriter writer, Chart chart, TimelineItem item, string id, int row, double left, double y, double width, double rowHeight, string duration, string summary) {
        var radius = Math.Min(ChartVisualPrimitives.TimelineItemCornerRadiusMax, rowHeight / 2);
        var highlightInset = Math.Min(radius, width / 3);
        var highlightEnd = left + width - highlightInset;
        var borderStroke = ChartVisualPrimitives.TimelineItemBorderStrokeWidth;
        var borderInset = borderStroke / 2.0;
        writer
            .StartElement("rect")
            .Attribute("data-cfx-role", "timeline-item")
            .Attribute("data-cfx-row", row)
            .Attribute("data-cfx-start", item.Start)
            .Attribute("data-cfx-end", item.End)
            .Attribute("data-cfx-duration", duration)
            .Attribute("role", "img")
            .Attribute("aria-label", summary)
            .Attribute("x", left)
            .Attribute("y", y)
            .Attribute("width", width)
            .Attribute("height", rowHeight)
            .Attribute("rx", radius)
            .Attribute("fill", "url(#" + id + "-timelineFill" + item.SeriesIndex.ToString(CultureInfo.InvariantCulture) + ")")
            .EndEmptyElement()
            .Line();
        writer
            .StartElement("rect")
            .Attribute("data-cfx-role", "timeline-item-border")
            .Attribute("x", left + borderInset)
            .Attribute("y", y + borderInset)
            .Attribute("width", Math.Max(0, width - borderStroke))
            .Attribute("height", Math.Max(0, rowHeight - borderStroke))
            .Attribute("rx", Math.Max(0, radius - borderInset))
            .Attribute("fill", "none")
            .Attribute("stroke", chart.Options.Theme.CardBackground.ToCss())
            .Attribute("stroke-opacity", ChartVisualPrimitives.TimelineItemBorderOpacity)
            .Attribute("stroke-width", borderStroke)
            .EndEmptyElement()
            .Line();
        if (highlightEnd > left + highlightInset) {
            writer
                .StartElement("line")
                .Attribute("data-cfx-role", "timeline-item-highlight")
                .Attribute("x1", left + highlightInset)
                .Attribute("y1", y + ChartVisualPrimitives.TimelineItemHighlightOffsetY)
                .Attribute("x2", highlightEnd)
                .Attribute("y2", y + ChartVisualPrimitives.TimelineItemHighlightOffsetY)
                .Attribute("stroke", "#fff")
                .Attribute("stroke-opacity", ChartVisualPrimitives.TimelineItemHighlightOpacity)
                .Attribute("stroke-width", ChartVisualPrimitives.GridStrokeWidth)
                .Attribute("stroke-linecap", "round")
                .EndEmptyElement()
                .Line();
        }
    }

    private static void DrawTimelineSvgXAxisTitle(SvgMarkupWriter writer, Chart chart, ChartRect plot, double y, string role) {
        if (string.IsNullOrWhiteSpace(chart.XAxisTitle)) return;
        DrawTimelineSvgTextCenteredX(writer, chart, role, chart.XAxisTitle, plot.Left + plot.Width / 2, y, chart.Options.Theme.MutedText, chart.Options.Theme.AxisTitleFontSize, plot.Width - 4, "600", middleBaseline: false, style: chart.Options.AxisTitleStyle);
    }

    private static void DrawTimelineSvgYAxisTitle(SvgMarkupWriter writer, Chart chart, ChartRect plot, double axisX, string role) {
        if (string.IsNullOrWhiteSpace(chart.YAxisTitle)) return;
        var t = chart.Options.Theme;
        var maxWidth = Math.Max(40, plot.Height * 0.72);
        var style = chart.Options.AxisTitleStyle;
        var fontSize = TextFontSizeForSvgWidth(chart.YAxisTitle, maxWidth, StyleFontSize(style, t.AxisTitleFontSize));
        var text = TrimSvgLabelToWidth(chart.YAxisTitle, fontSize, maxWidth);
        if (text.Length == 0) return;

        writer
            .StartElement("text")
            .Attribute("data-cfx-role", role)
            .Attribute("transform", "translate(" + F(axisX) + " " + F(plot.Top + plot.Height / 2) + ") rotate(-90)")
            .Attribute("text-anchor", "middle")
            .Attribute("fill", StyleColor(style, t.MutedText).ToCss())
            .Attribute("font-family", TimelineFontFamily(StyleFontFamily(chart, style)))
            .Attribute("font-size", fontSize)
            .Attribute("font-weight", StyleWeight(style, "600"));
        WriteTimelineSvgTextStyleAttributes(writer, style);
        writer
            .Text(text)
            .EndElement()
            .Line();
    }

    private static void DrawTimelineSvgTextCenteredX(SvgMarkupWriter writer, Chart chart, string role, string text, double centerX, double y, ChartColor fill, double fontSize, double maxWidth, string fontWeight, bool middleBaseline, ChartTextStyle? style) {
        var preferredFontSize = StyleFontSize(style, fontSize);
        var fittedFontSize = TextFontSizeForSvgWidth(text, Math.Max(8, maxWidth), preferredFontSize);
        var fittedText = TrimSvgLabelToWidth(text, fittedFontSize, Math.Max(8, maxWidth));
        if (fittedText.Length == 0) return;

        writer
            .StartElement("text")
            .Attribute("data-cfx-role", role)
            .Attribute("x", centerX)
            .Attribute("y", y)
            .Attribute("text-anchor", "middle");
        if (middleBaseline) writer.Attribute("dominant-baseline", "middle");
        writer
            .Attribute("fill", StyleColor(style, fill).ToCss())
            .Attribute("font-family", TimelineFontFamily(StyleFontFamily(chart, style)))
            .Attribute("font-size", fittedFontSize)
            .Attribute("font-weight", StyleWeight(style, fontWeight));
        WriteTimelineSvgTextStyleAttributes(writer, style);
        writer
            .Text(fittedText)
            .EndElement()
            .Line();
    }

    private static void WriteTimelineSvgTextStyleAttributes(SvgMarkupWriter writer, ChartTextStyle? style) {
        if (style == null) return;
        if (style.Italic) writer.Attribute("font-style", "italic");
        if (style.Underline) writer.Attribute("text-decoration", "underline");
    }

    private static string TimelineFontFamily(string value) =>
        string.IsNullOrWhiteSpace(value) ? "system-ui, sans-serif" : value;

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

    private static string BuildTimelineSummary(Chart chart, TimelineItem item, string duration) =>
        item.Name + ": " + FormatTimelineTick(chart, item.Start) + " to " + FormatTimelineTick(chart, item.End) + ", duration " + duration;

    private static ChartColor TimelineItemGradientTop(ChartColor color) => Blend(ChartColor.White, color, ChartVisualPrimitives.TimelineItemGradientTopBlend);

    private static ChartColor TimelineItemGradientBottom(ChartColor color) => Blend(ChartColor.Black, color, ChartVisualPrimitives.TimelineItemGradientBottomBlend);

    private static ChartColor GanttTaskGradientTop(ChartColor color) => Blend(ChartColor.White, color, ChartVisualPrimitives.GanttTaskGradientTopBlend);

    private static ChartColor GanttTaskGradientBottom(ChartColor color) => Blend(ChartColor.Black, color, ChartVisualPrimitives.GanttTaskGradientBottomBlend);

    private readonly struct TimelineItem {
        public TimelineItem(int seriesIndex, string name, double start, double end, ChartColor color, bool showDataLabels) {
            SeriesIndex = seriesIndex;
            Name = name;
            Start = start;
            End = end;
            Color = color;
            ShowDataLabels = showDataLabels;
        }

        public int SeriesIndex { get; }

        public string Name { get; }

        public double Start { get; }

        public double End { get; }

        public ChartColor Color { get; }

        public bool ShowDataLabels { get; }
    }
}
