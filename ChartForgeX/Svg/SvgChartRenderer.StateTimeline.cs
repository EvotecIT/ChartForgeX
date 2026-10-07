using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static bool IsStateTimelineChart(Chart chart) => ChartSeriesKindTraits.ContainsKind(chart, ChartSeriesKind.StateTimeline);

    private static void DrawStateTimeline(StringBuilder sb, Chart chart, ChartRect surface, string id) {
        var model = ChartStateTimelineModel.Build(chart);
        var bounds = ChartStateTimelineModel.ContentBounds(surface);
        var t = chart.Options.Theme;
        var tickStyle = chart.Options.TickLabelStyle;
        var tickFontSize = StyleFontSize(tickStyle, t.TickLabelFontSize);
        var legendStyle = chart.Options.LegendStyle;
        var legendFontSize = StyleFontSize(legendStyle, t.LegendFontSize);
        var laneLabelWidth = 0.0;
        var summaryWidth = model.SummaryHeader == null ? 0 : EstimateSvgStyledTextWidth(chart, model.SummaryHeader, tickFontSize, tickStyle, emphasized: true);
        foreach (var lane in model.Lanes) {
            laneLabelWidth = Math.Max(laneLabelWidth, EstimateSvgStyledTextWidth(chart, lane.Name, tickFontSize, tickStyle, emphasized: true));
            if (lane.Summary != null) summaryWidth = Math.Max(summaryWidth, EstimateSvgStyledTextWidth(chart, lane.Summary, tickFontSize, tickStyle, emphasized: true));
        }

        var tickLabels = model.Ticks.Select(model.FormatTick).ToArray();
        var axisLabelReserve = Math.Max(ChartStateTimelineModel.AxisReserve, SvgXAxisBottomReserve(chart, tickLabels, bounds.Width));
        var legend = chart.Options.ShowLegend
            ? model.LayoutLegend(text => EstimateSvgStyledTextWidth(chart, text, legendFontSize, legendStyle), bounds.Left, bounds.Width, ChartStateTimelineModel.LegendBudgetHeight(model.PlotArea(bounds, laneLabelWidth, summaryWidth, EstimateSvgStyledTextHeight(tickFontSize, tickStyle), 0, axisLabelReserve, 0).Height))
            : Array.Empty<ChartStateCategoryLegendItem>();
        var plot = model.PlotArea(bounds, laneLabelWidth, summaryWidth, EstimateSvgStyledTextHeight(tickFontSize, tickStyle), ChartStateTimelineModel.LegendHeight(chart, legend), axisLabelReserve, 0);
        var hatchId = id + "-stateHatch";
        // The timeline is written straight into the chart markup: nothing else appends to it until the writer completes.
        var writer = new SvgMarkupWriter(sb);
        writer.StartElement("g").Attribute("data-cfx-role", "state-timeline").EndStartElement().Line();
        WriteStateCategoryHatchPattern(writer, hatchId, chart);

        var range = new ChartRange();
        range.SetXBounds(model.Min, model.Max);
        var labels = model.Ticks.Select(tick => new ChartAxisLabel(tick, model.FormatTick(tick))).ToArray();
        var labelTicks = SelectXAxisTickValues(chart, range, plot, labels);

        foreach (var tick in model.Ticks) {
            var x = model.X(tick, plot);
            var gridStyle = chart.Options.GridLineStyle;
            if (chart.Options.ShowGrid && gridStyle.ShowVerticalLines && !t.UseGraphiteLayout) {
                var gridLine = new StringBuilder();
                WriteSvgGuideLine(gridLine, "state-timeline-grid", x, plot.Top, x, plot.Bottom, t.Grid.ToCss(), gridStyle.StrokeWidth, gridStyle.VerticalOpacity, gridStyle);
                writer.Raw(gridLine.ToString());
            }

            if (!ShowXAxis(chart) || !labelTicks.Contains(tick)) continue;
            var label = model.FormatTick(tick);
            var labelMarkup = new StringBuilder();
            var angle = Clamp(chart.Options.XAxisLabelAngle, -80, 80);
            DrawXAxisLabel(labelMarkup, chart, plot, label, x, plot.Bottom + XAxisLabelOffset(chart, tickLabels), angle, "state-timeline-tick-label", AxisTickLabelMaxWidth(plot, labelTicks.Count, angle), chart.Options.TryGetXAxisLabelHighlight(tick, out var highlight) ? highlight : (ChartColor?)null);
            writer.Raw(labelMarkup.ToString());
        }

        foreach (var group in model.Groups) {
            var top = model.GroupTop(plot, group);
            if (group.Offset > 0) {
                writer.StartElement("line").Attribute("data-cfx-role", "state-lane-group-rule").Attribute("x1", bounds.Left).Attribute("y1", top).Attribute("x2", bounds.Right).Attribute("y2", top)
                    .Attribute("stroke", t.Grid.ToCss()).Attribute("stroke-width", ChartVisualPrimitives.GridStrokeWidth).EndEmptyElement().Line();
            }

            if (ShowYAxis(chart) && group.Name.Length > 0) WriteStateCategoryText(writer, chart, "state-lane-group", TrimSvgLabelToWidth(chart, group.Name, tickFontSize, bounds.Width, tickStyle, emphasized: true), bounds.Left, top + model.GroupHeight(plot, group) / 2, "start", tickFontSize, tickStyle, "700", true, t.Text);
        }

        var band = model.LaneBand(plot);
        for (var laneIndex = 0; laneIndex < model.Lanes.Count; laneIndex++) {
            var lane = model.Lanes[laneIndex];
            var series = chart.Series[lane.SeriesIndex];
            var y = model.LaneTop(plot, laneIndex);
            writer.StartElement("rect").Attribute("data-cfx-role", "state-lane-track").Attribute("x", plot.Left).Attribute("y", y).Attribute("width", plot.Width).Attribute("height", band)
                .Attribute("rx", ChartStateTimelineModel.SegmentRadius).Attribute("fill", t.Grid.ToCss()).Attribute("opacity", 0.35).EndEmptyElement().Line();
            if (ShowYAxis(chart)) {
                var maxWidth = Math.Max(8, plot.Left - bounds.Left - ChartStateTimelineModel.ColumnGap);
                var fontSize = TextFontSizeForSvgWidth(chart, lane.Name, maxWidth, tickFontSize, tickStyle, emphasized: true);
                WriteStateCategoryText(writer, chart, "state-lane-label", TrimSvgLabelToWidth(chart, lane.Name, fontSize, maxWidth, tickStyle, emphasized: true), plot.Left - ChartStateTimelineModel.ColumnGap, y + band / 2, "end", fontSize, tickStyle, "600", true);
            }

            foreach (var segment in lane.Segments) {
                if (!model.TrySegmentSpan(segment, plot, out var left, out var width)) continue;
                var summary = model.SegmentSummary(lane, segment);
                var mark = ChartStateMark.For(chart, segment.State);
                writer.StartElement("rect")
                    .Attribute("data-cfx-role", "state-segment")
                    .Attribute("data-cfx-series", lane.SeriesIndex)
                    .Attribute("data-cfx-series-key", SeriesInteractionKey(series))
                    .Attribute("data-cfx-point", segment.PointIndex)
                    .Attribute("data-cfx-label", lane.Name + " · " + segment.State.Label)
                    .Attribute("data-cfx-status", segment.State.Key)
                    .Attribute("data-cfx-start", model.FormatInstant(segment.Start))
                    .Attribute("data-cfx-end", model.FormatInstant(segment.End))
                    .Attribute("data-cfx-meta-duration", ChartStateTimelineModel.FormatDuration(segment.End - segment.Start))
                    .Attribute("data-cfx-meta-detail", segment.Detail)
                    .Attribute("role", "img")
                    .Attribute("aria-label", summary)
                    .Attribute("x", left).Attribute("y", y).Attribute("width", width).Attribute("height", band)
                    .Attribute("rx", Math.Min(ChartStateTimelineModel.SegmentRadius, width / 2));
                WriteStateMarkFill(writer, mark)
                    .EndStartElement()
                    .StartElement("title").Text(summary).EndElement()
                    .EndElement().Line();
                WriteStateMarkLines(writer, hatchId, mark, left, y, width, band);
            }

            if (model.HasSummary && !string.IsNullOrWhiteSpace(lane.Summary)) {
                var summaryText = TrimSvgLabelToWidth(chart, lane.Summary!, tickFontSize, ChartStateTimelineModel.SummaryColumnWidth(bounds, summaryWidth), tickStyle, emphasized: true);
                WriteStateCategoryText(writer, chart, "state-lane-summary", summaryText, bounds.Right - 2, y + band / 2, "end", tickFontSize, tickStyle, "600", true, t.Text);
            }
        }

        if (model.HasSummary && !string.IsNullOrWhiteSpace(model.SummaryHeader)) {
            WriteStateCategoryText(writer, chart, "state-summary-header", TrimSvgLabelToWidth(chart, model.SummaryHeader!, tickFontSize, ChartStateTimelineModel.SummaryColumnWidth(bounds, summaryWidth), tickStyle, emphasized: true), bounds.Right - 2, plot.Top - ChartStateTimelineModel.SummaryHeaderOffset, "end", tickFontSize, tickStyle, "600", false);
        }

        if (ShowXAxis(chart)) {
            if (ShowXAxisLine(chart)) writer.StartElement("line").Attribute("data-cfx-role", "state-timeline-axis").Attribute("x1", plot.Left).Attribute("y1", plot.Bottom).Attribute("x2", plot.Right).Attribute("y2", plot.Bottom)
                .Attribute("stroke", t.Axis.ToCss()).Attribute("stroke-width", ChartVisualPrimitives.AxisStrokeWidth).EndEmptyElement().Line();
            var title = XAxisTitleText(chart);
            if (!string.IsNullOrWhiteSpace(title)) {
                DrawSvgTextCenteredX(writer, chart, "state-timeline-x-axis-title", title, plot.Left + plot.Width / 2, plot.Bottom + XAxisTitleOffset(chart, tickLabels), t.MutedText, StyleFontSize(chart.Options.AxisTitleStyle, t.AxisTitleFontSize), plot.Width - 4, "600", middleBaseline: false, style: chart.Options.AxisTitleStyle);
            }
        }

        WriteStateCategoryLegend(writer, chart, legend, bounds.Bottom - ChartStateTimelineModel.LegendHeight(chart, legend) + 4, hatchId, bounds);

        writer.EndElement().Line();
        writer.Complete();
    }

}
