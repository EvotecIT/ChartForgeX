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
    private static void DrawGantt(StringBuilder sb, Chart chart, ChartRect basePlot, string id) {
        var items = BuildGanttItems(chart);
        if (items.Count == 0) return;

        var t = chart.Options.Theme;
        var min = items.Min(item => item.Start);
        var max = items.Max(item => item.End);
        if (chart.Options.GanttToday.HasValue) {
            min = Math.Min(min, chart.Options.GanttToday.Value);
            max = Math.Max(max, chart.Options.GanttToday.Value);
        }

        ApplyTimelineAxisBounds(chart, ref min, ref max);
        var plot = ApplyGanttReserve(chart, basePlot, items);
        var rowHeight = Math.Max(18, Math.Min(30, plot.Height / items.Count * 0.52));
        var slotHeight = plot.Height / items.Count;
        var ticks = ChartTicks.Generate(min, max, Math.Min(7, Math.Max(3, chart.Options.TickCount)));
        var tickLabelWidth = Math.Max(18, plot.Width / Math.Max(1, ticks.Count - 1) - 6);
        var rowLabelWidth = Math.Max(8, plot.Left - 24);
        var rowCenters = new double[items.Count];
        var startXs = new double[items.Count];
        var endXs = new double[items.Count];

        var writer = new SvgMarkupWriter(4096);
        writer
            .StartElement("g")
            .Attribute("data-cfx-role", "gantt-chart")
            .EndStartElement()
            .Line();
        DrawGanttItemGradients(writer, id, items);
        foreach (var tick in ticks) {
            var x = ProjectTimelineX(tick, min, max, plot);
            if (chart.Options.ShowGrid) {
                writer
                    .StartElement("line")
                    .Attribute("x1", F(x))
                    .Attribute("y1", F(plot.Top))
                    .Attribute("x2", F(x))
                    .Attribute("y2", F(plot.Bottom))
                    .Attribute("stroke", t.Grid.ToCss())
                    .Attribute("stroke-width", F(ChartVisualPrimitives.GridStrokeWidth))
                    .Attribute("opacity", F(ChartVisualPrimitives.TimelineGridOpacity))
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
                    .Attribute("data-cfx-role", "gantt-tick-label")
                    .Attribute("x", F(labelX))
                    .Attribute("y", F(plot.Bottom + 22))
                    .Attribute("text-anchor", anchor)
                    .Attribute("fill", t.MutedText.ToCss())
                    .Attribute("font-family", GanttFontFamily(t.FontFamily))
                    .Attribute("font-size", F(labelFontSize))
                    .Raw(Escape(label))
                    .EndElement()
                    .Line();
            }
        }

        for (var i = 0; i < items.Count; i++) {
            var item = items[i];
            var centerY = plot.Top + i * slotHeight + slotHeight / 2;
            rowCenters[i] = centerY;
            startXs[i] = ProjectTimelineX(item.Start, min, max, plot);
            endXs[i] = ProjectTimelineX(item.End, min, max, plot);
            if (chart.Options.ShowGrid) {
                writer
                    .StartElement("line")
                    .Attribute("x1", F(plot.Left))
                    .Attribute("y1", F(centerY))
                    .Attribute("x2", F(plot.Right))
                    .Attribute("y2", F(centerY))
                    .Attribute("stroke", t.Grid.ToCss())
                    .Attribute("stroke-width", F(ChartVisualPrimitives.GridStrokeWidth))
                    .Attribute("opacity", F(ChartVisualPrimitives.TimelineRowGridOpacity))
                    .EndEmptyElement()
                    .Line();
            }

            if (chart.Options.ShowAxes) {
                var rowLabelFontSize = TextFontSizeForSvgWidth(item.Name, rowLabelWidth, t.TickLabelFontSize);
                var rowLabel = TrimSvgLabelToWidth(item.Name, rowLabelFontSize, rowLabelWidth);
                writer
                    .StartElement("text")
                    .Attribute("data-cfx-role", "gantt-row-label")
                    .Attribute("x", F(plot.Left - 14))
                    .Attribute("y", F(centerY))
                    .Attribute("text-anchor", "end")
                    .Attribute("dominant-baseline", "middle")
                    .Attribute("fill", t.MutedText.ToCss())
                    .Attribute("font-family", GanttFontFamily(t.FontFamily))
                    .Attribute("font-size", F(rowLabelFontSize))
                    .Attribute("font-weight", "650")
                    .Raw(Escape(rowLabel))
                    .EndElement()
                    .Line();
            }
        }

        for (var i = 0; i < items.Count; i++) {
            if (items[i].DependsOn >= 0 && items[i].DependsOn < i) DrawGanttDependency(writer, plot, endXs[items[i].DependsOn], rowCenters[items[i].DependsOn], startXs[i], rowCenters[i], t.Axis);
        }

        if (chart.Options.GanttToday.HasValue) DrawGanttToday(writer, chart, plot, min, max);

        for (var i = 0; i < items.Count; i++) {
            var item = items[i];
            if (item.Milestone) {
                DrawGanttMilestone(writer, chart, item, id, startXs[i], rowCenters[i], rowHeight);
                continue;
            }

            var left = Math.Min(startXs[i], endXs[i]);
            var width = Math.Max(2, Math.Abs(endXs[i] - startXs[i]));
            DrawGanttTask(writer, chart, item, id, i, left, rowCenters[i] - rowHeight / 2, width, rowHeight);
        }

        if (chart.Options.ShowAxes) {
            writer
                .StartElement("line")
                .Attribute("x1", F(plot.Left))
                .Attribute("y1", F(plot.Bottom))
                .Attribute("x2", F(plot.Right))
                .Attribute("y2", F(plot.Bottom))
                .Attribute("stroke", t.Axis.ToCss())
                .Attribute("stroke-width", F(ChartVisualPrimitives.AxisStrokeWidth))
                .EndEmptyElement()
                .Line();
            DrawGanttSvgXAxisTitle(writer, chart, plot, plot.Bottom + 49, "gantt-x-axis-title");
            if (!string.IsNullOrWhiteSpace(chart.YAxisTitle)) {
                var widestLabel = items.Max(item => EstimateTextWidth(item.Name, t.TickLabelFontSize));
                DrawGanttSvgYAxisTitle(writer, chart, plot, Math.Max(24, plot.Left - widestLabel - 46), "gantt-y-axis-title");
            }
        }

        writer.EndElement().Line();
        sb.Append(writer.Build());
    }

    private static void DrawGanttItemGradients(SvgMarkupWriter writer, string id, IReadOnlyList<GanttItem> items) {
        writer.StartElement("defs").EndStartElement().Line();
        foreach (var item in items) {
            writer
                .StartElement("linearGradient")
                .Attribute("id", id + "-ganttFill" + item.SeriesIndex.ToString(CultureInfo.InvariantCulture))
                .Attribute("x1", "0")
                .Attribute("x2", "0")
                .Attribute("y1", "0")
                .Attribute("y2", "1")
                .EndStartElement()
                .StartElement("stop")
                .Attribute("offset", "0%")
                .Attribute("stop-color", GanttTaskGradientTop(item.Color).ToHex())
                .EndEmptyElement()
                .StartElement("stop")
                .Attribute("offset", "100%")
                .Attribute("stop-color", GanttTaskGradientBottom(item.Color).ToHex())
                .EndEmptyElement()
                .EndElement()
                .Line();
        }
        writer.EndElement().Line();
    }

    private static List<GanttItem> BuildGanttItems(Chart chart) {
        var items = new List<GanttItem>();
        for (var i = 0; i < chart.Series.Count; i++) {
            var series = chart.Series[i];
            if (series.Kind != ChartSeriesKind.Gantt || series.Points.Count < 3) continue;
            var range = series.Points[0];
            var metadata = series.Points[1];
            var flags = series.Points[2];
            items.Add(new GanttItem(i, series.Name, Math.Min(range.X, range.Y), Math.Max(range.X, range.Y), Clamp(metadata.X, 0, 1), (int)Math.Round(metadata.Y), flags.X >= 0.5, series.Color ?? Color(chart, i), ShouldDrawDataLabels(chart, series)));
        }

        return items;
    }

    private static void DrawGanttTask(SvgMarkupWriter writer, Chart chart, GanttItem item, string id, int row, double left, double y, double width, double height) {
        var t = chart.Options.Theme;
        var radius = Math.Min(ChartVisualPrimitives.GanttTaskCornerRadiusMax, height / 2);
        var progressWidth = Math.Max(0, width * item.Progress);
        var summary = item.Name + ": " + FormatTimelineTick(chart, item.Start) + " to " + FormatTimelineTick(chart, item.End) + ", " + FormatPercent(item.Progress) + " complete";
        var borderStroke = ChartVisualPrimitives.GanttTaskBorderStrokeWidth;
        var borderInset = borderStroke / 2.0;
        writer
            .StartElement("rect")
            .Attribute("data-cfx-role", "gantt-task")
            .Attribute("data-cfx-row", row)
            .Attribute("data-cfx-start", F(item.Start))
            .Attribute("data-cfx-end", F(item.End))
            .Attribute("data-cfx-progress", F(item.Progress))
            .Attribute("role", "img")
            .Attribute("aria-label", summary)
            .Attribute("x", F(left))
            .Attribute("y", F(y))
            .Attribute("width", F(width))
            .Attribute("height", F(height))
            .Attribute("rx", F(radius))
            .Attribute("fill", item.Color.ToCss())
            .Attribute("opacity", F(ChartVisualPrimitives.GanttTaskTrackOpacity))
            .EndEmptyElement()
            .Line();
        if (progressWidth > 0.5) {
            writer
                .StartElement("rect")
                .Attribute("data-cfx-role", "gantt-progress")
                .Attribute("x", F(left))
                .Attribute("y", F(y))
                .Attribute("width", F(progressWidth))
                .Attribute("height", F(height))
                .Attribute("rx", F(radius))
                .Attribute("fill", "url(#" + id + "-ganttFill" + item.SeriesIndex.ToString(CultureInfo.InvariantCulture) + ")")
                .EndEmptyElement()
                .Line();
        }

        writer
            .StartElement("rect")
            .Attribute("data-cfx-role", "gantt-task-border")
            .Attribute("x", F(left + borderInset))
            .Attribute("y", F(y + borderInset))
            .Attribute("width", F(Math.Max(0, width - borderStroke)))
            .Attribute("height", F(Math.Max(0, height - borderStroke)))
            .Attribute("rx", F(Math.Max(0, radius - borderInset)))
            .Attribute("fill", "none")
            .Attribute("stroke", t.CardBackground.ToCss())
            .Attribute("stroke-opacity", F(ChartVisualPrimitives.GanttTaskBorderOpacity))
            .Attribute("stroke-width", F(borderStroke))
            .EndEmptyElement()
            .Line();
        var inset = Math.Min(radius, width / 3);
        if (width > inset * 2 + 3) {
            writer
                .StartElement("line")
                .Attribute("data-cfx-role", "gantt-task-highlight")
                .Attribute("x1", F(left + inset))
                .Attribute("y1", F(y + ChartVisualPrimitives.GanttTaskHighlightOffsetY))
                .Attribute("x2", F(left + width - inset))
                .Attribute("y2", F(y + ChartVisualPrimitives.GanttTaskHighlightOffsetY))
                .Attribute("stroke", "#fff")
                .Attribute("stroke-opacity", F(ChartVisualPrimitives.GanttTaskHighlightOpacity))
                .Attribute("stroke-width", F(ChartVisualPrimitives.GridStrokeWidth))
                .Attribute("stroke-linecap", "round")
                .EndEmptyElement()
                .Line();
        }

        if (item.ShowDataLabels && width >= 74) DrawGanttSvgTextCenteredX(writer, chart, "gantt-progress-label", FormatPercent(item.Progress), left + width / 2, y + height / 2, HeatmapTextColor(item.Color), t.DataLabelFontSize, width - 8, "750", t.CardBackground, 2.2);
    }

    private static void DrawGanttMilestone(SvgMarkupWriter writer, Chart chart, GanttItem item, string id, double x, double centerY, double rowHeight) {
        var size = Math.Max(8, rowHeight * 0.44);
        var summary = item.Name + ": milestone at " + FormatTimelineTick(chart, item.Start);
        var points = F(x) + "," + F(centerY - size) + " " + F(x + size) + "," + F(centerY) + " " + F(x) + "," + F(centerY + size) + " " + F(x - size) + "," + F(centerY);
        writer
            .StartElement("polygon")
            .Attribute("data-cfx-role", "gantt-milestone")
            .Attribute("data-cfx-start", F(item.Start))
            .Attribute("role", "img")
            .Attribute("aria-label", summary)
            .Attribute("points", points)
            .Attribute("fill", "url(#" + id + "-ganttFill" + item.SeriesIndex.ToString(CultureInfo.InvariantCulture) + ")")
            .Attribute("stroke", chart.Options.Theme.CardBackground.ToCss())
            .Attribute("stroke-opacity", F(ChartVisualPrimitives.GanttTaskBorderOpacity))
            .Attribute("stroke-width", F(ChartVisualPrimitives.GanttTaskBorderStrokeWidth))
            .EndEmptyElement()
            .Line();
    }

    private static void DrawGanttDependency(SvgMarkupWriter writer, ChartRect plot, double fromX, double fromY, double toX, double toY, ChartColor color) {
        var midX = Clamp(fromX + Math.Max(ChartVisualPrimitives.GanttDependencyMinMidOffset, (toX - fromX) / 2), plot.Left, plot.Right);
        var endX = Clamp(toX - ChartVisualPrimitives.GanttDependencyEndpointInset, plot.Left, plot.Right);
        var targetX = Clamp(toX, plot.Left, plot.Right);
        var path = "M " + F(Clamp(fromX + ChartVisualPrimitives.GanttDependencyEndpointInset, plot.Left, plot.Right)) + " " + F(fromY) + " H " + F(midX) + " V " + F(toY) + " H " + F(endX);
        writer
            .StartElement("path")
            .Attribute("data-cfx-role", "gantt-dependency")
            .Attribute("d", path)
            .Attribute("fill", "none")
            .Attribute("stroke", color.ToCss())
            .Attribute("stroke-width", F(ChartVisualPrimitives.GanttDependencyStrokeWidth))
            .Attribute("stroke-linecap", "round")
            .Attribute("stroke-linejoin", "round")
            .Attribute("stroke-dasharray", F(ChartVisualPrimitives.GanttDependencyDash) + " " + F(ChartVisualPrimitives.GanttDependencyGap))
            .Attribute("opacity", F(ChartVisualPrimitives.GanttDependencyOpacity))
            .EndEmptyElement()
            .Line();
        writer
            .StartElement("path")
            .Attribute("data-cfx-role", "gantt-dependency-arrow")
            .Attribute("d", "M " + F(endX) + " " + F(toY - ChartVisualPrimitives.GanttDependencyArrowSize) + " L " + F(targetX) + " " + F(toY) + " L " + F(endX) + " " + F(toY + ChartVisualPrimitives.GanttDependencyArrowSize))
            .Attribute("fill", "none")
            .Attribute("stroke", color.ToCss())
            .Attribute("stroke-width", F(ChartVisualPrimitives.GanttDependencyStrokeWidth))
            .Attribute("stroke-linecap", "round")
            .Attribute("stroke-linejoin", "round")
            .Attribute("opacity", F(ChartVisualPrimitives.GanttDependencyOpacity))
            .EndEmptyElement()
            .Line();
    }

    private static void DrawGanttToday(SvgMarkupWriter writer, Chart chart, ChartRect plot, double min, double max) {
        var t = chart.Options.Theme;
        var x = ProjectTimelineX(chart.Options.GanttToday!.Value, min, max, plot);
        if (x < plot.Left || x > plot.Right) return;
        writer
            .StartElement("line")
            .Attribute("data-cfx-role", "gantt-today")
            .Attribute("x1", F(x))
            .Attribute("y1", F(plot.Top))
            .Attribute("x2", F(x))
            .Attribute("y2", F(plot.Bottom))
            .Attribute("stroke", t.Warning.ToCss())
            .Attribute("stroke-width", F(ChartVisualPrimitives.GanttTodayStrokeWidth))
            .Attribute("stroke-dasharray", "6 5")
            .EndEmptyElement()
            .Line();
        DrawGanttSvgTextCenteredX(writer, chart, "gantt-today-label", "Today", x, plot.Top - 8, t.Warning, t.TickLabelFontSize, 62, "750", t.CardBackground, 2.2, middleBaseline: false);
    }

    private static void DrawGanttSvgXAxisTitle(SvgMarkupWriter writer, Chart chart, ChartRect plot, double y, string role) {
        if (string.IsNullOrWhiteSpace(chart.XAxisTitle)) return;
        DrawGanttSvgTextCenteredX(writer, chart, role, chart.XAxisTitle, plot.Left + plot.Width / 2, y, chart.Options.Theme.MutedText, chart.Options.Theme.AxisTitleFontSize, plot.Width - 4, "600", middleBaseline: false, style: chart.Options.AxisTitleStyle);
    }

    private static void DrawGanttSvgYAxisTitle(SvgMarkupWriter writer, Chart chart, ChartRect plot, double axisX, string role) {
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
            .Attribute("font-family", GanttFontFamily(StyleFontFamily(chart, style)))
            .Attribute("font-size", F(fontSize))
            .Attribute("font-weight", StyleWeight(style, "600"));
        WriteGanttSvgTextStyleAttributes(writer, style);
        writer
            .Raw(Escape(text))
            .EndElement()
            .Line();
    }

    private static void DrawGanttSvgTextCenteredX(SvgMarkupWriter writer, Chart chart, string role, string text, double centerX, double y, ChartColor fill, double fontSize, double maxWidth, string fontWeight, ChartColor? stroke = null, double strokeWidth = 0, bool middleBaseline = true, ChartTextStyle? style = null) {
        var preferredFontSize = StyleFontSize(style, fontSize);
        var fittedFontSize = TextFontSizeForSvgWidth(text, Math.Max(8, maxWidth), preferredFontSize);
        var fittedText = TrimSvgLabelToWidth(text, fittedFontSize, Math.Max(8, maxWidth));
        if (fittedText.Length == 0) return;

        writer
            .StartElement("text")
            .Attribute("data-cfx-role", role)
            .Attribute("x", F(centerX))
            .Attribute("y", F(y))
            .Attribute("text-anchor", "middle");
        if (middleBaseline) writer.Attribute("dominant-baseline", "middle");
        writer.Attribute("fill", StyleColor(style, fill).ToCss());
        if (stroke.HasValue && strokeWidth > 0) {
            writer
                .Attribute("stroke", stroke.Value.ToCss())
                .Attribute("stroke-width", F(strokeWidth))
                .Attribute("paint-order", "stroke fill")
                .Attribute("stroke-linejoin", "round");
        }

        writer
            .Attribute("font-family", GanttFontFamily(StyleFontFamily(chart, style)))
            .Attribute("font-size", F(fittedFontSize))
            .Attribute("font-weight", StyleWeight(style, fontWeight));
        WriteGanttSvgTextStyleAttributes(writer, style);
        writer
            .Raw(Escape(fittedText))
            .EndElement()
            .Line();
    }

    private static void WriteGanttSvgTextStyleAttributes(SvgMarkupWriter writer, ChartTextStyle? style) {
        if (style == null) return;
        if (style.Italic) writer.Attribute("font-style", "italic");
        if (style.Underline) writer.Attribute("text-decoration", "underline");
    }

    private static string GanttFontFamily(string value) =>
        string.IsNullOrWhiteSpace(value) ? "system-ui, sans-serif" : value;

    private static ChartRect ApplyGanttReserve(Chart chart, ChartRect plot, IReadOnlyList<GanttItem> items) {
        var t = chart.Options.Theme;
        var widest = items.Max(item => EstimateTextWidth(item.Name, t.TickLabelFontSize));
        var yAxisReserve = string.IsNullOrWhiteSpace(chart.YAxisTitle) ? 0 : 28;
        var desiredLeft = Math.Max(plot.Left, widest + yAxisReserve + 64);
        var maxLeft = Math.Max(plot.Left, chart.Options.Size.Width - chart.Options.Padding.Right - 220);
        var shift = Math.Max(0, Math.Min(desiredLeft, maxLeft) - plot.Left);
        var topShift = chart.Options.GanttToday.HasValue ? 12 : 0;
        var bottomReserve = 52 + (string.IsNullOrWhiteSpace(chart.XAxisTitle) ? 0 : 18);
        return new ChartRect(plot.X + shift, plot.Y + topShift, Math.Max(1, plot.Width - shift), Math.Max(1, plot.Height - bottomReserve - topShift));
    }

    private readonly struct GanttItem {
        public GanttItem(int seriesIndex, string name, double start, double end, double progress, int dependsOn, bool milestone, ChartColor color, bool showDataLabels) {
            SeriesIndex = seriesIndex;
            Name = name;
            Start = start;
            End = end;
            Progress = progress;
            DependsOn = dependsOn;
            Milestone = milestone;
            Color = color;
            ShowDataLabels = showDataLabels;
        }

        public int SeriesIndex { get; }
        public string Name { get; }
        public double Start { get; }
        public double End { get; }
        public double Progress { get; }
        public int DependsOn { get; }
        public bool Milestone { get; }
        public ChartColor Color { get; }
        public bool ShowDataLabels { get; }
    }
}
