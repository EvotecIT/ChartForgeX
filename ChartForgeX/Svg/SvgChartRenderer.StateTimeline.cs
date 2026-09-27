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
            : Array.Empty<ChartStateTimelineLegendItem>();
        var plot = model.PlotArea(bounds, laneLabelWidth, summaryWidth, EstimateSvgStyledTextHeight(tickFontSize, tickStyle), ChartStateTimelineModel.LegendHeight(chart, legend), axisLabelReserve, 0);
        var hatchId = id + "-stateHatch";
        var writer = new SvgMarkupWriter(8192);
        writer.StartElement("g").Attribute("data-cfx-role", "state-timeline").EndStartElement().Line();
        writer.StartElement("defs").EndStartElement()
            .StartElement("pattern").Attribute("id", hatchId).Attribute("width", ChartStateTimelineModel.HatchSpacing).Attribute("height", ChartStateTimelineModel.HatchSpacing).Attribute("patternUnits", "userSpaceOnUse").Attribute("patternTransform", "rotate(45)").EndStartElement()
            .StartElement("line").Attribute("x1", 0).Attribute("y1", 0).Attribute("x2", 0).Attribute("y2", ChartStateTimelineModel.HatchSpacing).Attribute("stroke", "#fff").Attribute("stroke-opacity", ChartStateTimelineModel.HatchOpacity).Attribute("stroke-width", 1.5).EndEmptyElement()
            .EndElement().EndElement().Line();

        var range = new ChartRange();
        range.SetXBounds(model.Min, model.Max);
        var labels = model.Ticks.Select(tick => new ChartAxisLabel(tick, model.FormatTick(tick))).ToArray();
        var labelTicks = SelectXAxisTickValues(chart, range, plot, labels);

        foreach (var tick in model.Ticks) {
            var x = model.X(tick, plot);
            var gridStyle = chart.Options.GridLineStyle;
            if (chart.Options.ShowGrid && gridStyle.ShowVerticalLines) {
                var gridLine = new StringBuilder();
                WriteSvgGuideLine(gridLine, "state-timeline-grid", x, plot.Top, x, plot.Bottom, t.Grid.ToCss(), gridStyle.StrokeWidth, gridStyle.VerticalOpacity, gridStyle);
                writer.Raw(gridLine.ToString());
            }

            if (!ShowXAxis(chart) || !labelTicks.Contains(tick)) continue;
            var label = model.FormatTick(tick);
            var labelMarkup = new StringBuilder();
            var angle = Clamp(chart.Options.XAxisLabelAngle, -80, 80);
            DrawXAxisLabel(labelMarkup, chart, plot, label, x, plot.Bottom + XAxisLabelOffset(chart, tickLabels), angle, "state-timeline-tick-label", AxisTickLabelMaxWidth(plot, labelTicks.Count, angle));
            writer.Raw(labelMarkup.ToString());
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
                WriteStateTimelineText(writer, chart, "state-lane-label", TrimSvgLabelToWidth(chart, lane.Name, fontSize, maxWidth, tickStyle, emphasized: true), plot.Left - ChartStateTimelineModel.ColumnGap, y + band / 2, "end", fontSize, tickStyle, "600", true);
            }

            foreach (var segment in lane.Segments) {
                if (!model.TrySegmentSpan(segment, plot, out var left, out var width)) continue;
                var summary = model.SegmentSummary(lane, segment);
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
                    .Attribute("rx", Math.Min(ChartStateTimelineModel.SegmentRadius, width / 2))
                    .Attribute("fill", segment.State.Color.ToCss())
                    .EndStartElement()
                    .StartElement("title").Text(summary).EndElement()
                    .EndElement().Line();
                if (segment.State.Hatched) WriteStateTimelineHatch(writer, hatchId, left, y, width, band);
            }

            if (model.HasSummary && !string.IsNullOrWhiteSpace(lane.Summary)) {
                var summaryText = TrimSvgLabelToWidth(chart, lane.Summary!, tickFontSize, ChartStateTimelineModel.SummaryColumnWidth(bounds, summaryWidth), tickStyle, emphasized: true);
                WriteStateTimelineText(writer, chart, "state-lane-summary", summaryText, bounds.Right - 2, y + band / 2, "end", tickFontSize, tickStyle, "600", true, t.Text);
            }
        }

        if (model.HasSummary && !string.IsNullOrWhiteSpace(model.SummaryHeader)) {
            WriteStateTimelineText(writer, chart, "state-summary-header", TrimSvgLabelToWidth(chart, model.SummaryHeader!, tickFontSize, ChartStateTimelineModel.SummaryColumnWidth(bounds, summaryWidth), tickStyle, emphasized: true), bounds.Right - 2, plot.Top - 8, "end", tickFontSize, tickStyle, "600", false);
        }

        if (ShowXAxis(chart)) {
            if (ShowXAxisLine(chart)) writer.StartElement("line").Attribute("data-cfx-role", "state-timeline-axis").Attribute("x1", plot.Left).Attribute("y1", plot.Bottom).Attribute("x2", plot.Right).Attribute("y2", plot.Bottom)
                .Attribute("stroke", t.Axis.ToCss()).Attribute("stroke-width", ChartVisualPrimitives.AxisStrokeWidth).EndEmptyElement().Line();
            var title = XAxisTitleText(chart);
            if (!string.IsNullOrWhiteSpace(title)) {
                DrawSvgTextCenteredX(writer, chart, "state-timeline-x-axis-title", title, plot.Left + plot.Width / 2, plot.Bottom + XAxisTitleOffset(chart, tickLabels), t.MutedText, StyleFontSize(chart.Options.AxisTitleStyle, t.AxisTitleFontSize), plot.Width - 4, "600", middleBaseline: false, style: chart.Options.AxisTitleStyle);
            }
        }

        var legendTop = bounds.Bottom - ChartStateTimelineModel.LegendHeight(chart, legend) + 4;
        foreach (var item in legend) {
            var rowY = legendTop + item.Row * LegendRowBudget.RowHeight(chart);
            var rowCenter = rowY + LegendRowBudget.RowHeight(chart) / 2;
            if (item.Omitted > 0) {
                DrawLegendOverflow(writer, chart, new ChartRect(bounds.Left, rowY, bounds.Width, LegendRowBudget.RowHeight(chart)), rowCenter + EstimateSvgStyledTextHeight(legendFontSize, legendStyle) / 2, item.Omitted);
                continue;
            }

            var state = item.State!;
            var swatchY = rowCenter - ChartStateTimelineModel.LegendSwatch / 2;
            writer.StartElement("rect").Attribute("data-cfx-role", "state-legend-swatch").Attribute("data-cfx-status", state.Key).Attribute("x", item.X).Attribute("y", swatchY)
                .Attribute("width", ChartStateTimelineModel.LegendSwatch).Attribute("height", ChartStateTimelineModel.LegendSwatch).Attribute("rx", ChartStateTimelineModel.SegmentRadius).Attribute("fill", state.Color.ToCss()).EndEmptyElement().Line();
            if (state.Hatched) WriteStateTimelineHatch(writer, hatchId, item.X, swatchY, ChartStateTimelineModel.LegendSwatch, ChartStateTimelineModel.LegendSwatch);
            var labelWidth = Math.Max(8, bounds.Right - item.X - ChartStateTimelineModel.LegendSwatch - 6);
            var label = TrimSvgLabelToWidth(chart, state.Label, legendFontSize, labelWidth, legendStyle);
            WriteStateTimelineText(writer, chart, "state-legend-label", label, item.X + ChartStateTimelineModel.LegendSwatch + 6, rowCenter, "start", legendFontSize, legendStyle, "400", true);
        }

        writer.EndElement().Line();
        sb.Append(writer.Build());
    }

    private static void WriteStateTimelineHatch(SvgMarkupWriter writer, string hatchId, double x, double y, double width, double height) {
        writer.StartElement("rect").Attribute("data-cfx-role", "state-segment-hatch").Attribute("x", x).Attribute("y", y).Attribute("width", width).Attribute("height", height)
            .Attribute("rx", Math.Min(ChartStateTimelineModel.SegmentRadius, width / 2)).Attribute("fill", "url(#" + hatchId + ")").Attribute("pointer-events", "none").EndEmptyElement().Line();
    }

    private static void WriteStateTimelineText(SvgMarkupWriter writer, Chart chart, string role, string text, double x, double y, string anchor, double fontSize, TextStyleOverride style, string weight, bool middle, ChartColor? color = null) {
        if (text.Length == 0) return;
        writer.StartElement("text").Attribute("data-cfx-role", role).Attribute("x", x).Attribute("y", y).Attribute("text-anchor", anchor);
        if (middle) writer.Attribute("dominant-baseline", "middle");
        writer.Attribute("fill", StyleColor(style, color ?? chart.Options.Theme.MutedText).ToCss())
            .Attribute("font-family", SvgFontFamilyAttributeValue(StyleFontFamily(chart, style)))
            .Attribute("font-size", fontSize)
            .Attribute("font-weight", StyleWeight(style, weight));
        WriteSvgTextStyleAttributes(writer, style);
        WriteSvgStyledTextContent(writer, style, text).EndElement().Line();
    }
}
